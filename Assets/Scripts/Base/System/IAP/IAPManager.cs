using Spenve;
using System;
using System.Collections.Generic;
using UnityEngine;

#if Purchase
using UnityEngine.Purchasing;

namespace Spenve
{
    public class IAPProduct
    {
        public string id;

        public int type;

        // pid, name
        public IDs storeItem = new IDs();

        public IAPProduct(string id1, int type1)
        {
            this.id = id1;
            this.type = type1;
        }
    }

    public class IAPManager : ScriptSingleton<IAPManager>, ISystem, IStoreListener
    {
        private static float TIME_OUT = 60.0f;

        private bool initSuccess = false;

        private IStoreController controller;
        private IExtensionProvider extensions;

        private bool inPurchase = false;

        private bool inRestore = false;

        private bool inInit = false;

        private float timeOutTime = 0;

        //only local valid this time!!!
        private bool remoteValid = false;

        public string remoteValidURL = "";

        private List<string> products=null;
        public int purchaseFlag = 1;

        private Action<int, IAPProduct> OnFinishCallback = null;

        //private List<string> lastPurchases = null;

        private Action<int> OnInitOver = null;

        private Dictionary<string, IAPProduct> productConfigs = new Dictionary<string, IAPProduct>();

#region ISystem
        [NoToLuaAttribute]
        public void Reset()
        {
        }

         [NoToLuaAttribute]
        public void Load()
        {
        }

        [NoToLuaAttribute]
        public void Launch()
        {
        }

        [NoToLuaAttribute]
        public void Dispose()
        {
        }

        [NoToLuaAttribute]
        public void Update() 
        {
            if (inInit && timeOutTime > 0 )
            {
                if ( Time.realtimeSinceStartup > timeOutTime)
                {
                    timeOutTime = -1.0f;
                    OnInitOver(-6);
                }
            }
        }

        #endregion

        public void AddProduct(string id, int type, string[] items) 
        {
            IAPProduct iap = productConfigs.ContainsKey(id)?productConfigs[id]: new IAPProduct(id,type);

            for (int i = 0; i < items.Length; i += 2 )
            {
                string pid = items[i];
                string store = items[i + 1];

                iap.storeItem.Add(pid, store);
            }

            productConfigs[id] = iap;
        }

        public void Init(Action<int> callback) 
        {
            if (inInit)
            {
                //start waiting again
                if( timeOutTime < 0 )
                    timeOutTime = Time.realtimeSinceStartup + TIME_OUT;

                OnInitOver = callback;
                //callback(-6);
                return;
            }

            timeOutTime = Time.realtimeSinceStartup + TIME_OUT;

            OnInitOver = callback;

            inInit = true;

            try
            {
                var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());

                var it = productConfigs.GetEnumerator();
                while( it.MoveNext() )
                {
                    IAPProduct iap = it.Current.Value;

                    builder.AddProduct(iap.id, (ProductType)iap.type, iap.storeItem);
                }
 
                UnityPurchasing.Initialize(this, builder);
            }
            catch (Exception)
            {
                inInit = false;
                OnInitOver(-1);
            }
            
        }

        public void Restore(Action<int, IAPProduct> onRestoreItem, Action<int> onResult)
        {
            if (Application.platform == RuntimePlatform.IPhonePlayer || Application.platform == RuntimePlatform.OSXPlayer)
            {
                if (!initSuccess)
                {
                    onResult(-4);
                    return;
                }

                Debug.Log("restore on IPhonePlayer ");

                inRestore = true;
                OnFinishCallback = onRestoreItem;

                extensions.GetExtension<IAppleExtensions>().RestoreTransactions(result =>
                {
                    if (result)
                    {
                        // This does not mean anything was restored,
                        // merely that the restoration process succeeded.
                        onResult(0);
                    }
                    else
                    {
                        // Restoration failed.
                        onResult(-4);
                    }

                    inRestore = false;
                });
            }
            else if (Application.platform == RuntimePlatform.Android)
            {
                if (!initSuccess)
                {
                    onResult(-4);
                    return;
                }

                Debug.Log("restore on android ");

                inRestore = true;
                OnFinishCallback = onRestoreItem;

                Product[] list = controller.products.all;
                if (list != null)
                {
                    for (int i = 0; i < list.Length; ++i)
                    {
                        Product pro = list[i];
                        if (pro.availableToPurchase && pro.hasReceipt)
                        {
                            OnFinishCallback(0, new IAPProduct(pro.definition.id, (int)pro.definition.type));
                        }
                    }
                }

                inRestore = false;
            }
            else
            {
                onResult(-5);
            }
        }

        public void Purchase(string id, Action<int, IAPProduct> callback) 
        {
            OnFinishCallback = callback;

            IAPProduct errorProduct = new IAPProduct(id, 0);

            if (!initSuccess)
            {
                OnFinishCallback(-2, errorProduct);
                return;
            }


            if (inPurchase || inRestore)
            {
                OnFinishCallback(-3, errorProduct);
                return;
            }

            Product pro = controller.products.WithID(id);
            if (pro == null || !pro.availableToPurchase)
            {
                OnFinishCallback(3, errorProduct);
                return;
            }

            if (pro.hasReceipt)
            {
                OnFinishCallback(0, new IAPProduct(pro.definition.id, (int)pro.definition.type));       
                return;
            }
            else
            {
                inPurchase = true;
                try
                {
                    Debug.Log("try to purchase " + pro.definition.id + " with id=" + pro.definition.storeSpecificId);
                    controller.InitiatePurchase(pro);  
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
                        
            }
        }


        [NoToLuaAttribute]
        public LuaTable toLuaTable(IList<string> objs)
        {
            var table = (LuaTable)ScripSystem.Instance.luaState.DoString("return {}")[0];
            foreach (var obj in objs)
            {
                string[] infs = obj.Split('-');
                table[infs[0]] = infs[1];
            }
            return table;
        }

        public LuaTable GetProductsPrice()
        {
            if (initSuccess)
            {
                //IAP初始化成功
                return toLuaTable(products);
            }
            return null;
        }


#region IStoreListener
        [NoToLuaAttribute]
        public void OnInitializeFailed(InitializationFailureReason error)
        {
            initSuccess = false;
            inInit = false;
            OnInitOver(GetInitializedErrorCode(error));

        }

        private int GetInitializedErrorCode( InitializationFailureReason error ) 
        {
            if (error == InitializationFailureReason.PurchasingUnavailable)
                return 1;
            else if (error == InitializationFailureReason.NoProductsAvailable)
                return 2;
            else if (error == InitializationFailureReason.AppNotKnown)
                return 3;
            return 4;
        }
        [NoToLuaAttribute]
        public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
        {      
            this.controller = controller;
            this.extensions = extensions;

            initSuccess = true;
            inInit = false;

            //读出 product id
            products = new List<string>();
            foreach (var item in controller.products.all)
            {
                if (item.availableToPurchase)
                {
                    string title = item.definition.id; //item.metadata.localizedTitle;
                    //if (title.Contains("Fake title for "))
                    //{
                    //    title = title.Substring(15);
                    //}
                    string temp = string.Join("-",
                        new[]
					{
						title,
                        //item.metadata.localizedPrice.ToString(),
						item.metadata.localizedPriceString
					});

                    products.Add(temp);
                    Debug.Log(temp);
                }
            }


            OnInitOver(0);
        }
        [NoToLuaAttribute]
        public void OnPurchaseFailed(Product product, PurchaseFailureReason error)
        {
            inPurchase = false;

            IAPProduct errorP = null;
            if (product == null)
            {
                errorP = new IAPProduct("", 0);
            }
            else
            {
                errorP = new IAPProduct(product.definition.id, (int)product.definition.type);
            }

            OnFinishCallback((int)error, errorP);
        }
        [NoToLuaAttribute]
        public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs e)
        {
            if (remoteValid)
            {
                doRemoteValid(e.purchasedProduct);
                return PurchaseProcessingResult.Pending;
            }
            else
            {
                doPurchaseOver(e.purchasedProduct);
                return PurchaseProcessingResult.Complete;
            }
        }

        private void doPurchaseOver(Product product)
        {
            inPurchase = false;

            if (product == null)
            {
                Debug.Log("there is no prodcut ");
            }
            else
            {
                IAPProduct result = new IAPProduct(product.definition.id, (int)product.definition.type);

                if (OnFinishCallback != null)
                {
                    OnFinishCallback(0, result);
                }
                else
                {
                    Debug.Log("there is no OnFinishCallback ");
                }
            }
        }

        private void doRemoteValid(Product product)
        {
            throw new NotImplementedException();
        }




        #endregion
    }
}


#else

namespace Spenve
{
    public class IAPProduct
    {
        public string id;

        public int type;

        public IAPProduct(string id1, int type1)
        {
            this.id = id1;
            this.type = type1;
        }
    }


    public class IAPManager : SystemSingleton<IAPManager>
    {
        public int purchaseFlag = 0;
        
        public void Reset()
        {
            //throw new NotImplementedException();
        }

        public void Load()
        {
            //throw new NotImplementedException();
        }

        public void Launch()
        {
            //throw new NotImplementedException();
        }

        public void Dispose()
        {
            //throw new NotImplementedException();
        }


        public void AddProduct(string id, int type, string[] items)
        {
        }

        public void Init(Action<int> callback)
        {
            callback(0);
        }

        public void Restore(Action<int, IAPProduct> onRestoreItem, Action<int> onResult)
        {
            onResult(0);
        }

        public void Purchase(string id, Action<int, IAPProduct> callback)
        {
            callback(0, new IAPProduct(id,0));
        }

    }
}

#endif