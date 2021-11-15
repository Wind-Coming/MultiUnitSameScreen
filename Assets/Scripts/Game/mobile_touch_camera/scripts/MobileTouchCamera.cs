// /************************************************************
// *                                                           *
// *   Mobile Touch Camera                                     *
// *                                                           *
// *   Created 2015 by BitBender Games                         *
// *                                                           *
// *   bitbendergames@gmail.com                                *
// *                                                           *
// ************************************************************/

using System.Collections.Generic;
using UnityEngine;
using System.Collections;

namespace BitBenderGames
{

    [RequireComponent(typeof(TouchInputController))]
    [RequireComponent(typeof(Camera))]
    public class MobileTouchCamera : MonoBehaviourWrapped
    {

        #region inspector
        [SerializeField]
        [Tooltip("You need to define whether your camera is a side-view camera (which is the default when using the 2D mode of unity) or if you chose a top-down looking camera. This parameter tells the system whether to scroll in XY direction, or in XZ direction.")]
        private CameraPlaneAxes cameraAxes = CameraPlaneAxes.XY_2D_SIDESCROLL;
        [SerializeField]
        [Tooltip("When using a perspective camera, the zoom can either be performed by changing the field of view, or by moving the camera closer to the scene.")]
        private PerspectiveZoomMode perspectiveZoomMode = PerspectiveZoomMode.FIELD_OF_VIEW;
        [SerializeField]
        [Tooltip("For perspective cameras this value denotes the min field of view used for zooming (field of view zoom), or the min distance to the ground (translation zoom). For orthographic cameras it denotes the min camera size.")]
        private float camZoomMin = 4;
        [SerializeField]
        [Tooltip("For perspective cameras this value denotes the max field of view used for zooming (field of view zoom), or the max distance to the ground (translation zoom). For orthographic cameras it denotes the max camera size.")]
        private float camZoomMax = 12;
        [SerializeField]
        [Tooltip("The cam will overzoom the min/max values by this amount and spring back when the user releases the zoom.")]
        private float camOverzoomMargin = 1;
        [SerializeField]
        [Tooltip("When dragging the camera close to the defined border, it will spring back when the user stops dragging. This value defines the distance from the border where the camera will spring back to.")]
        private float camOverdragMargin = 5.0f;
        [SerializeField]
        private bool useBoundaryOrViewCenter = true;

        [SerializeField]
        [Tooltip("These values define the scrolling borders for the camera. The camera will not scroll further than defined here. When a top-down camera is used, these 2 values are applied to the X/Z position.")]
        private Vector2 boundaryMin = new Vector2(-1000, -1000);
        [SerializeField]
        [Tooltip("These values define the scrolling borders for the camera. The camera will not scroll further than defined here. When a top-down camera is used, these 2 values are applied to the X/Z position.")]
        private Vector2 boundaryMax = new Vector2(1000, 1000);
        [Header("Advanced")]
        [SerializeField]
        [Tooltip("The lower the value, the slower the camera will follow. The higher the value, the more direct the camera will follow movement updates. Necessary for keeping the camera smooth when the framerate is not in sync with the touch input update rate.")]
        private float camFollowFactor = 15.0f;
        [SerializeField]
        [Tooltip("Set the behaviour of the damping (e.g. the slow-down) at the end of auto-scrolling.")]
#pragma warning disable 414 //NOTE: This field is actually used by the custom inspector. The pragma disables the warning that appears because the variable isn't accessed directly, but only through reflection.
        private AutoScrollDampMode autoScrollDampMode = AutoScrollDampMode.DEFAULT;
#pragma warning restore 414
        [SerializeField]
        [Tooltip("When dragging quickly, the camera will keep autoscrolling in the last direction. The autoscrolling will slowly come to a halt. This value defines how fast the camera will come to a halt.")]
        private float autoScrollDamp = 300;
        [SerializeField]
        [Tooltip("This curve allows to modulate the auto scroll damp value over time.")]
        private AnimationCurve autoScrollDampCurve = new AnimationCurve(new Keyframe(0, 1, 0, 0), new Keyframe(0.7f, 0.9f, -0.5f, -0.5f), new Keyframe(1, 0.01f, -0.85f, -0.85f));
        [SerializeField]
        [Tooltip("The camera assumes that the scrollable content of your scene (e.g. the ground of your game-world) is located at y = 0 for top-down cameras or at z = 0 for side-scrolling cameras. In case this is not valid for your scene, you may adjust this property to the correct offset.")]
        private float groundLevelOffset = 0;
        [SerializeField]
        [Tooltip("When enabled, the camera can be rotated using a 2-finger rotation gesture.")]
        private bool enableRotation = false;
        [SerializeField]
        [Tooltip("When enabled, the camera can be tilted using a synced 2-finger up or down motion.")]
        private bool enableTilt = false;
        [SerializeField]
        [Tooltip("The minimum tilt angle for the camera.")]
        private float tiltAngleMin = 45;
        [SerializeField]
        [Tooltip("The maximum tilt angle for the camera.")]
        private float tiltAngleMax = 90;
        [SerializeField]
        [Tooltip("When enabled, the camera is tilted automatically when zooming.")]
        private bool enableZoomTilt = false;
        [SerializeField]
        [Tooltip("The minimum tilt angle for the camera when using zoom tilt.")]
        private float zoomTiltAngleMin = 45;
        [SerializeField]
        [Tooltip("The maximum tilt angle for the camera when using zoom tilt.")]
        private float zoomTiltAngleMax = 90;
        [Header("Event Callbacks")]
        [SerializeField]
        [Tooltip("Here you can set up callbacks to be invoked when an item with Collider is tapped on.")]
        private UnityEventWithRaycastHit OnPickItem;
        [SerializeField]
        [Tooltip("Here you can set up callbacks to be invoked when an item with Collider2D is tapped on.")]
        private UnityEventWithRaycastHit2D OnPickItem2D;
        [SerializeField]
        [Tooltip("Here you can set up callbacks to be invoked when an item with Collider is double-tapped on.")]
        private UnityEventWithRaycastHit OnPickItemDoubleClick;
        [SerializeField]
        [Tooltip("Here you can set up callbacks to be invoked when an item with Collider2D is double-tapped on.")]
        private UnityEventWithRaycastHit2D OnPickItem2DDoubleClick;
        #endregion

        public CameraPlaneAxes CameraAxes
        {
            get { return (cameraAxes); }
            set { cameraAxes = value; }
        }

        private TouchInputController touchInputController;

        private Vector3 dragStartCamPos;
        private Vector3 cameraScrollVelocity;

        private float pinchStartCamZoomSize;
        private Vector3 pinchStartIntersectionCenter;
        private Vector3 pinchCenterCurrent;
        private float pinchDistanceCurrent;
        private float pinchAngleCurrent = 0;
        private float pinchDistanceStart;
        private Vector3 pinchCenterCurrentLerp;
        private float pinchDistanceCurrentLerp;
        private float pinchAngleCurrentLerp;
        private bool isRotationLock = true;
        private bool isRotationActivated = false;
        private float pinchAngleLastFrame = 0;
        private float pinchTiltCurrent = 0;
        private float pinchTiltAccumulated = 0;
        private bool isTiltModeEvaluated = false;
        private float pinchTiltLastFrame;
        private bool isPinchTiltMode;

        private float timeRealDragStop;

        public bool IsAutoScrolling { get { return (cameraScrollVelocity.sqrMagnitude > float.Epsilon); } }

        public bool IsPinching { get; private set; }
        public bool IsDragging { get; private set; }

        #region expert mode tweakables
        [Header("Expert Mode")]
        [SerializeField]
        private bool expertModeEnabled;
        [SerializeField]
        [Tooltip("Depending on your settings the camera allows to zoom slightly over the defined value. When releasing the zoom the camera will spring back to the defined value. This variable defines the speed of the spring back.")]
        private float zoomBackSpringFactor = 20;
        [SerializeField]
        [Tooltip("When close to the border the camera will spring back if the margin is bigger than 0. This variable defines the speed of the spring back.")]
        private float dragBackSpringFactor = 10;
        [SerializeField]
        [Tooltip("When swiping over the screen the camera will keep scrolling a while before coming to a halt. This variable limits the maximum velocity of the auto scroll.")]
        private float autoScrollVelocityMax = 60;
        [SerializeField]
        [Tooltip("This value defines how quickly the camera comes to a halt when auto scrolling.")]
        private float dampFactorTimeMultiplier = 2;
        [SerializeField]
        [Tooltip("When setting this flag to true, the camera will behave like a popular tower defense game. It will either go into an exclusive tilt mode, or into a combined zoom/rotate mode. When set to false, the camera will behave like a popular city building game. The camera won't pan with 2 fingers, and instead zoom, rotate and tilt are done in parallel.")]
        private bool isPinchModeExclusive = true;
        [SerializeField]
        [Tooltip("This value should be kept at 1 for pixel perfect zoom. In case you need a non-pixel perfect, slower or faster zoom however, you can change this value. 0.5f for example will make the camera zoom half as fast as in pixel perfect mode. This value is currently tested only in perspective camera mode with translation based zoom.")]
        private float customZoomSensitivity = 1.0f;
        [SerializeField]
        [Tooltip("Optional. When assigned, the terrain collider will be used to align items on the ground following the terrain.")]
        private TerrainCollider terrainCollider;
        [SerializeField]
        [Tooltip("Optional. When assigned, the given transform will be moved and rotated instead of the one where this component is located on.")]
        private Transform cameraTransform;
        #endregion

        #region cam rotation tweakables
        [SerializeField]
        [Tooltip("A gesture may be interpreted as intended rotation in case the relative rotation angle between 2 frames becomes bigger than this value.")]
        private float rotationDetectionDeltaThreshold = 0.25f;
        [SerializeField]
        [Tooltip("Relative pinch distance must be bigger than this value in order to detect a rotation. This is to prevent errors that occur when the fingers are too close together to properly detect a clean rotation.")]
        private float rotationMinPinchDistance = 0.125f;
        [SerializeField]
        [Tooltip("The rotation mode is enabled as soon as the rotation by the user becomes bigger than this value (in degrees). The value is used to prevent micro rotations from regular jittering of the fingers to be interpreted as rotation and helps keeping the camera more steady and less jittery.")]
        private float rotationLockThreshold = 2.5f;
        #endregion

        #region tilt tweakables
        [SerializeField]
        [Tooltip("After this amount of finger-movement (relative to screen size), the pinch mode is decided. E.g. whether tilt mode or regular mode is used.")]
        private float pinchModeDetectionMoveTreshold = 0.025f;
        [SerializeField]
        [Tooltip("A threshold used to detect the up or down tilting motion.")]
        private float pinchTiltModeThreshold = 0.0075f;
        [SerializeField]
        [Tooltip("The tilt sensitivity once the tilt mode has started.")]
        private float pinchTiltSpeed = 180;
        #endregion

        private bool isStarted = false;

        public Camera Cam { get; private set; }

        private bool IsTranslationZoom { get { return (Cam.orthographic == false && perspectiveZoomMode == PerspectiveZoomMode.TRANSLATION); } }

        public float CamZoom
        {
            get
            {
                if (Cam.orthographic == true)
                {
                    return Cam.orthographicSize;
                }
                else
                {
                    if (IsTranslationZoom == true)
                    {
                        Vector3 camCenterIntersection = GetIntersectionPoint(GetCamCenterRay());
                        return (Vector3.Distance(camCenterIntersection, Transform.position));
                    }
                    else
                    {
                        return Cam.fieldOfView;
                    }
                }
            }
            set
            {
                if (Cam.orthographic == true)
                {
                    Cam.orthographicSize = value;
                }
                else
                {
                    if (IsTranslationZoom == true)
                    {
                        Vector3 camCenterIntersection = GetIntersectionPoint(GetCamCenterRay());
                        Transform.position = camCenterIntersection - Transform.forward * value;
                        ComputeViewPos();
                    }
                    else
                    {
                        Cam.fieldOfView = value;
                    }
                }
                ComputeCamBoundaries();
                Spenve.MsgSystem.Instance.PostMessage("OnZoomChanged", (int)Transform.position.y);
            }
        }

        public float CamZoomMin
        {
            get { return (camZoomMin); }
            set { camZoomMin = value; }
        }
        public float CamZoomMax
        {
            get { return (camZoomMax); }
            set { camZoomMax = value; }
        }
        public float CamOverzoomMargin
        {
            get { return (camOverzoomMargin); }
            set { camOverzoomMargin = value; }
        }
        public float CamFollowFactor
        {
            get { return (camFollowFactor); }
            set { camFollowFactor = value; }
        }
        public float AutoScrollDamp
        {
            get { return autoScrollDamp; }
            set { autoScrollDamp = value; }
        }
        public AnimationCurve AutoScrollDampCurve
        {
            get { return (autoScrollDampCurve); }
            set { autoScrollDampCurve = value; }
        }
        public float GroundLevelOffset
        {
            get { return groundLevelOffset; }
            set { groundLevelOffset = value; }
        }
        public Vector2 BoundaryMin
        {
            get { return boundaryMin; }
            set { boundaryMin = value; }
        }
        public Vector2 BoundaryMax
        {
            get { return boundaryMax; }
            set { boundaryMax = value; }
        }
        public PerspectiveZoomMode PerspectiveZoomMode
        {
            get { return (perspectiveZoomMode); }
            set { perspectiveZoomMode = value; }
        }
        public bool EnableRotation
        {
            get { return enableRotation; }
            set { enableRotation = value; }
        }
        public bool EnableTilt
        {
            get { return enableTilt; }
            set { enableTilt = value; }
        }
        public float TiltAngleMin
        {
            get { return tiltAngleMin; }
            set { tiltAngleMin = value; }
        }
        public float TiltAngleMax
        {
            get { return tiltAngleMax; }
            set { tiltAngleMax = value; }
        }
        public bool EnableZoomTilt
        {
            get { return enableZoomTilt; }
            set { enableZoomTilt = value; }
        }
        public float ZoomTiltAngleMin
        {
            get { return zoomTiltAngleMin; }
            set { zoomTiltAngleMin = value; }
        }
        public float ZoomTiltAngleMax
        {
            get { return zoomTiltAngleMax; }
            set { zoomTiltAngleMax = value; }
        }

        private bool isDraggingSceneObject;

        private Plane refPlaneXY = new Plane(new Vector3(0, 0, -1), 0);
        private Plane refPlaneXZ = new Plane(new Vector3(0, 1, 0), 0);
        public Plane RefPlane
        {
            get
            {
                if (CameraAxes == CameraPlaneAxes.XZ_TOP_DOWN)
                {
                    return refPlaneXZ;
                }
                else
                {
                    return refPlaneXY;
                }
            }
        }

        private List<Vector3> DragCameraMoveVector { get; set; }
        private const int momentumSamplesCount = 5;

        private const float pinchDistanceForTiltBreakout = 0.05f;
        private const float pinchAccumBreakout = 0.025f;

        private Vector3 targetPositionClamped = Vector3.zero;
        private Vector3 oldPosition = Vector3.zero;

        public bool IsSmoothingEnabled { get; set; }

        private float ScreenRatio { get; set; }

        private Vector2 CamPosMin { get; set; }
        private Vector2 CamPosMax { get; set; }

        public TerrainCollider TerrainCollider
        {
            get { return terrainCollider; }
            set { terrainCollider = value; }
        }

        #region work in progress //Features that are currently being worked on, but not fully polished and documented yet. Use them at your own risk.
        private bool enableOvertiltSpring = false; //Allows to enable the camera to spring being when being tilted over the limits.
        private float camOvertiltMargin = 5.0f;
        private float tiltBackSpringFactor = 30;
        private float minOvertiltSpringPositionThreshold = 0.1f; //This value is necessary to reposition the camera and do boundary update computations while the auto spring back from overtilt is active and larger than this value.
        #endregion

        public void Awake()
        {
            if (cameraTransform != null)
            {
                cachedTransform = cameraTransform;
            }
            Cam = GetComponent<Camera>();

            IsSmoothingEnabled = false;
            touchInputController = GetComponent<TouchInputController>();
            dragStartCamPos = Vector3.zero;
            cameraScrollVelocity = Vector3.zero;
            timeRealDragStop = 0;
            pinchStartCamZoomSize = 0;
            IsPinching = false;
            IsDragging = false;
            DragCameraMoveVector = new List<Vector3>();
            refPlaneXY = new Plane(new Vector3(0, 0, -1), groundLevelOffset);
            refPlaneXZ = new Plane(new Vector3(0, 1, 0), -groundLevelOffset);
            ScreenRatio = GetScreenRatio();
            if (EnableZoomTilt == true)
            {
                ResetZoomTilt();
            }
            ComputeCamBoundaries();

            if (CamZoomMax < CamZoomMin)
            {
                Debug.LogWarning("The defined max camera zoom (" + CamZoomMax + ") is smaller than the defined min (" + CamZoomMin + "). Automatically switching the values.");
                float camZoomMinBackup = CamZoomMin;
                CamZoomMin = CamZoomMax;
                CamZoomMax = camZoomMinBackup;
            }

            //Errors for certain incorrect settings.
            string cameraAxesError = CheckCameraAxesErrors();
            if (string.IsNullOrEmpty(cameraAxesError) == false)
            {
                Debug.LogError(cameraAxesError);
            }
        }

        public void Start()
        {
            touchInputController.OnInputClick += InputControllerOnInputClick;
            touchInputController.OnDragStart += InputControllerOnDragStart;
            touchInputController.OnDragUpdate += InputControllerOnDragUpdate;
            touchInputController.OnDragStop += InputControllerOnDragStop;
            touchInputController.OnFingerDown += InputControllerOnFingerDown;
            touchInputController.OnFingerUp += InputControllerOnFingerUp;
            touchInputController.OnPinchStart += InputControllerOnPinchStart;
            touchInputController.OnPinchUpdateExtended += InputControllerOnPinchUpdate;
            touchInputController.OnPinchStop += InputControllerOnPinchStop;
            isStarted = true;
            StartCoroutine(InitCamBoundariesDelayed());
        }

        private IEnumerator InitCamBoundariesDelayed()
        {
            yield return null;
            ComputeCamBoundaries();
        }

        public void OnDestroy()
        {
            if (isStarted)
            {
                touchInputController.OnInputClick -= InputControllerOnInputClick;
                touchInputController.OnDragStart -= InputControllerOnDragStart;
                touchInputController.OnDragUpdate -= InputControllerOnDragUpdate;
                touchInputController.OnDragStop -= InputControllerOnDragStop;
                touchInputController.OnFingerDown -= InputControllerOnFingerDown;
                touchInputController.OnFingerUp -= InputControllerOnFingerUp;
                touchInputController.OnPinchStart -= InputControllerOnPinchStart;
                touchInputController.OnPinchUpdateExtended -= InputControllerOnPinchUpdate;
                touchInputController.OnPinchStop -= InputControllerOnPinchStop;
            }
        }

        /// <summary>
        /// MonoBehaviour method override to assign proper default values depending on
        /// the camera parameters and orientation.
        /// </summary>
        private void Reset()
        {

            //Compute camera tilt to find out the camera orientation.
            Vector3 camForwardOnPlane = Vector3.Cross(Vector3.up, GetTiltRotationAxis());
            float tiltAngle = Vector3.Angle(camForwardOnPlane, -Transform.forward);
            if (tiltAngle < 45)
            {
                CameraAxes = CameraPlaneAxes.XY_2D_SIDESCROLL;
            }
            else
            {
                CameraAxes = CameraPlaneAxes.XZ_TOP_DOWN;
            }

            //Compute zoom default values based on the camera type.
            Camera cameraComponent = GetComponent<Camera>();
            if (cameraComponent.orthographic == true)
            {
                CamZoomMin = 4;
                CamZoomMax = 13;
                CamOverzoomMargin = 1;
            }
            else
            {
                CamZoomMin = 5;
                CamZoomMax = 40;
                CamOverzoomMargin = 3;
                PerspectiveZoomMode = PerspectiveZoomMode.TRANSLATION;
            }
        }

        /// <summary>
        /// Method for resetting the camera boundaries. This method may need to be invoked
        /// when resetting the camera transform (rotation, tilt) by code for example.
        /// </summary>
        public void ResetCameraBoundaries()
        {
            ComputeCamBoundaries();
        }

        /// <summary>
        /// This method tilts the camera based on the values
        /// defined for the zoom tilt mode.
        /// </summary>
        public void ResetZoomTilt()
        {
            UpdateTiltForAutoTilt(CamZoom);
        }

        /// <summary>
        /// Helper method for retrieving the world position of the
        /// finger with id 0. This method may only return a valid value when
        /// there is at least 1 finger touching the device.
        /// </summary>
        public Vector3 GetFinger0PosWorld()
        {
            Vector3 posWorld = Vector3.zero;
            if (TouchWrapper.TouchCount > 0)
            {
                Vector3 fingerPos = TouchWrapper.Touch0.Position;
                RaycastGround(Cam.ScreenPointToRay(fingerPos), out posWorld);
            }
            return (posWorld);
        }

        /// <summary>
        /// Method for performing a raycast against either the refplane, or
        /// against a terrain-collider in case the collider is set.
        /// </summary>
        public bool RaycastGround(Ray ray, out Vector3 hitPoint)
        {
            bool hitSuccess = false;
            hitPoint = Vector3.zero;
            if (TerrainCollider != null)
            {
                RaycastHit hitInfo;
                hitSuccess = TerrainCollider.Raycast(ray, out hitInfo, Mathf.Infinity);
                if (hitSuccess == true)
                {
                    hitPoint = hitInfo.point;
                }
            }
            else
            {
                float hitDistance = 0;
                hitSuccess = RefPlane.Raycast(ray, out hitDistance);
                if (hitSuccess == true)
                {
                    hitPoint = ray.GetPoint(hitDistance);
                }
            }
            return hitSuccess;
        }

        /// <summary>
        /// Method for retrieving the intersection-point between the given ray and the ref plane.
        /// </summary>
        public Vector3 GetIntersectionPoint(Ray ray)
        {
            float distance = 0;
            bool success = RefPlane.Raycast(ray, out distance);
            if (success == false)
            {
                Debug.LogError("Failed to compute intersection between camera ray and reference plane. Make sure the camera Axes are set up correctly.");
            }
            return (ray.origin + ray.direction * distance);
        }

        /// <summary>
        /// Custom planet intersection method that doesn't take into account rays parallel to the plane or rays shooting in the wrong direction and thus never hitting.
        /// May yield slightly better performance however and should be safe for use when the camera setup is correct (e.g. axes set correctly in this script, and camera actually pointing towards floor).
        /// </summary>
        public Vector3 GetIntersectionPointUnsafe(Ray ray)
        {
            float distance = Vector3.Dot(RefPlane.normal, Vector3.zero - ray.origin) / Vector3.Dot(RefPlane.normal, (ray.origin + ray.direction) - ray.origin);
            return (ray.origin + ray.direction * distance);
        }

        /// <summary>
        /// Method that does all the computation necessary when the pinch gesture of the user
        /// has changed.
        /// </summary>
        private void UpdatePinch(float deltaTime)
        {

            if (IsPinching == true)
            {

                if (isTiltModeEvaluated == true)
                {

                    if (isPinchTiltMode == true || isPinchModeExclusive == false)
                    {

                        //Tilt
                        float pinchTiltDelta = pinchTiltLastFrame - pinchTiltCurrent;
                        UpdateCameraTilt(pinchTiltDelta * pinchTiltSpeed);
                        pinchTiltLastFrame = pinchTiltCurrent;

                    }
                    if (isPinchTiltMode == false || isPinchModeExclusive == false)
                    {

                        if (isRotationActivated == true && isRotationLock == true && Mathf.Abs(pinchAngleCurrent) >= rotationLockThreshold)
                        {
                            isRotationLock = false;
                        }

                        if (IsSmoothingEnabled == true)
                        {
                            float lerpFactor = Mathf.Clamp01(Time.deltaTime * camFollowFactor);
                            pinchDistanceCurrentLerp = Mathf.Lerp(pinchDistanceCurrentLerp, pinchDistanceCurrent, lerpFactor);
                            pinchCenterCurrentLerp = Vector3.Lerp(pinchCenterCurrentLerp, pinchCenterCurrent, lerpFactor);
                            if (isRotationLock == false)
                            {
                                pinchAngleCurrentLerp = Mathf.Lerp(pinchAngleCurrentLerp, pinchAngleCurrent, lerpFactor);
                            }
                        }
                        else
                        {
                            pinchDistanceCurrentLerp = pinchDistanceCurrent;
                            pinchCenterCurrentLerp = pinchCenterCurrent;
                            if (isRotationLock == false)
                            {
                                pinchAngleCurrentLerp = pinchAngleCurrent;
                            }
                        }

                        //Rotation
                        if (isRotationActivated == true && isRotationLock == false)
                        {
                            float pinchAngleDelta = pinchAngleCurrentLerp - pinchAngleLastFrame;
                            Vector3 rotationAxis = GetRotationAxis();
                            Transform.RotateAround(pinchCenterCurrent, rotationAxis, pinchAngleDelta);
                            pinchAngleLastFrame = pinchAngleCurrentLerp;
                            ComputeCamBoundaries();
                        }

                        //Zoom
                        float zoomFactor = (pinchDistanceStart / Mathf.Max(((pinchDistanceCurrentLerp - pinchDistanceStart) * customZoomSensitivity) + pinchDistanceStart, 0.0001f));
                        float cameraSize = pinchStartCamZoomSize * zoomFactor;
                        cameraSize = Mathf.Clamp(cameraSize, camZoomMin - camOverzoomMargin, camZoomMax + camOverzoomMargin);
                        if (enableZoomTilt == true)
                        {
                            UpdateTiltForAutoTilt(cameraSize);
                        }
                        CamZoom = cameraSize;
                    }

                    //Position update.
                    DoPositionUpdateForTilt(false);
                }
            }
            else
            {
                //Spring back.
                if (EnableTilt == true && enableOvertiltSpring == true)
                {
                    float overtiltSpringValue = ComputeOvertiltSpringBackFactor(camOvertiltMargin);
                    if (Mathf.Abs(overtiltSpringValue) > minOvertiltSpringPositionThreshold)
                    {
                        UpdateCameraTilt(overtiltSpringValue * deltaTime * tiltBackSpringFactor);
                        DoPositionUpdateForTilt(true);
                    }
                }
            }
        }

        private void UpdateTiltForAutoTilt(float newCameraSize)
        {
            float zoomProgress = Mathf.Clamp01((newCameraSize - camZoomMin) / (camZoomMax - camZoomMin));
            float tiltTarget = Mathf.Lerp(zoomTiltAngleMin, zoomTiltAngleMax, zoomProgress);
            float tiltAngleDiff = tiltTarget - GetCurrentTiltAngleDeg(GetTiltRotationAxis());
            UpdateCameraTilt(tiltAngleDiff);
        }

        /// <summary>
        /// Method that computes the updated camera position when the user tilts the camera.
        /// </summary>
        private void DoPositionUpdateForTilt(bool isSpringBack)
        {

            //Position update.
            Vector3 intersectionDragCurrent;
            if (isSpringBack == true || (isPinchTiltMode == true && isPinchModeExclusive == true))
            {
                intersectionDragCurrent = GetIntersectionPoint(GetCamCenterRay()); //In exclusive tilt mode always rotate around the screen center.
            }
            else
            {
                intersectionDragCurrent = GetIntersectionPoint(Cam.ScreenPointToRay(pinchCenterCurrentLerp));
            }
            Vector3 dragUpdateVector = intersectionDragCurrent - pinchStartIntersectionCenter;
            if (isSpringBack == true && isPinchModeExclusive == false)
            {
                dragUpdateVector = Vector3.zero;
            }
            Vector3 targetPos = GetClampToBoundaries(Transform.position - dragUpdateVector);

            Transform.position = targetPos; //Disable smooth follow for the pinch-move update to prevent oscillation during the zoom phase.
            ComputeViewPos();
            SetTargetPosition(targetPos);
        }

        /// <summary>
        /// Helper method for computing the tilt spring back.
        /// </summary>
        private float ComputeOvertiltSpringBackFactor(float margin)
        {

            float springBackValue = 0;
            Vector3 rotationAxis = GetTiltRotationAxis();
            float tiltAngle = GetCurrentTiltAngleDeg(rotationAxis);
            if (tiltAngle < tiltAngleMin + margin)
            {
                springBackValue = (tiltAngleMin + margin) - tiltAngle;
            }
            else if (tiltAngle > tiltAngleMax - margin)
            {
                springBackValue = (tiltAngleMax - margin) - tiltAngle;
            }
            return springBackValue;
        }

        /// <summary>
        /// Method that computes all necessary parameters for a tilt update caused by the user's tilt gesture.
        /// </summary>
        private void UpdateCameraTilt(float angle)
        {
            Vector3 rotationAxis = GetTiltRotationAxis();
            Vector3 rotationPoint = GetIntersectionPoint(new Ray(Transform.position, Transform.forward));
            Transform.RotateAround(rotationPoint, rotationAxis, angle);
            ClampCameraTilt(rotationPoint, rotationAxis);
            ComputeCamBoundaries();
        }

        /// <summary>
        /// Method that ensures that all limits are met when the user tilts the camera.
        /// </summary>
        private void ClampCameraTilt(Vector3 rotationPoint, Vector3 rotationAxis)
        {

            float tiltAngle = GetCurrentTiltAngleDeg(rotationAxis);
            if (tiltAngle < tiltAngleMin)
            {
                float tiltClampDiff = tiltAngleMin - tiltAngle;
                Transform.RotateAround(rotationPoint, rotationAxis, tiltClampDiff);
            }
            else if (tiltAngle > tiltAngleMax)
            {
                float tiltClampDiff = tiltAngleMax - tiltAngle;
                Transform.RotateAround(rotationPoint, rotationAxis, tiltClampDiff);
            }
        }

        /// <summary>
        /// Method to get the current tilt angle of the camera.
        /// </summary>
        private float GetCurrentTiltAngleDeg(Vector3 rotationAxis)
        {
            Vector3 camForwardOnPlane = Vector3.Cross(RefPlane.normal, rotationAxis);
            float tiltAngle = Vector3.Angle(camForwardOnPlane, -Transform.forward);
            return (tiltAngle);
        }

        /// <summary>
        /// Returns the rotation axis of the camera. This purely depends
        /// on the defined camera axis.
        /// </summary>
        private Vector3 GetRotationAxis()
        {
            return (RefPlane.normal);
        }

        /// <summary>
        /// Returns the rotation of the camera.
        /// </summary>
        private float GetRotationDeg()
        {
            if (CameraAxes == CameraPlaneAxes.XY_2D_SIDESCROLL)
            {
                return (Transform.rotation.eulerAngles.z);
            }
            else
            {
                return (Transform.rotation.eulerAngles.y);
            }
        }

        /// <summary>
        /// Returns the tilt rotation axis.
        /// </summary>
        private Vector3 GetTiltRotationAxis()
        {
            Vector3 rotationAxis = Transform.right;
            return (rotationAxis);
        }

        /// <summary>
        /// Method to compute all the necessary updates when the user moves the camera.
        /// </summary>
        private void UpdatePosition(float deltaTime)
        {

            if (IsPinching == true && isPinchTiltMode == true)
            {
                return;
            }

            if (IsDragging == true || IsPinching == true)
            {
                Vector3 posOld = Transform.position;
                if (IsSmoothingEnabled == true)
                {
                    Transform.position = Vector3.Lerp(Transform.position, targetPositionClamped, Mathf.Clamp01(Time.deltaTime * camFollowFactor));
                }
                else
                {
                    Transform.position = targetPositionClamped;
                }
                DragCameraMoveVector.Add((posOld - Transform.position) / Time.deltaTime);
                if (DragCameraMoveVector.Count > momentumSamplesCount)
                {
                    DragCameraMoveVector.RemoveAt(0);
                }
            }

            Vector2 autoScrollVector = -cameraScrollVelocity * deltaTime;
            Vector3 camPos = Transform.position;
            switch (cameraAxes)
            {
                case CameraPlaneAxes.XY_2D_SIDESCROLL:
                    camPos.x += autoScrollVector.x;
                    camPos.y += autoScrollVector.y;
                    break;
                case CameraPlaneAxes.XZ_TOP_DOWN:
                    camPos.x += autoScrollVector.x;
                    camPos.z += autoScrollVector.y;
                    break;
            }

            if (IsDragging == false && IsPinching == false)
            {
                Vector3 overdragSpringVector = ComputeOverdragSpringBackVector(camPos, camOverdragMargin, ref cameraScrollVelocity);
                if (overdragSpringVector.magnitude > float.Epsilon)
                {
                    camPos += Time.deltaTime * overdragSpringVector * dragBackSpringFactor;
                }
            }

            Transform.position = GetClampToBoundaries(camPos);
            ComputeViewPos();
        }

        /// <summary>
        /// Computes the camera drag spring back when the user is close to a boundary.
        /// </summary>
        private Vector3 ComputeOverdragSpringBackVector(Vector3 camPos, float margin, ref Vector3 currentCamScrollVelocity)
        {
            Vector3 springBackVector = Vector3.zero;
            if (camPos.x < CamPosMin.x + margin)
            {
                springBackVector.x = (CamPosMin.x + margin) - camPos.x;
                currentCamScrollVelocity.x = 0;
            }
            else if (camPos.x > CamPosMax.x - margin)
            {
                springBackVector.x = (CamPosMax.x - margin) - camPos.x;
                currentCamScrollVelocity.x = 0;
            }

            switch (cameraAxes)
            {
                case CameraPlaneAxes.XY_2D_SIDESCROLL:
                    if (camPos.y < CamPosMin.y + margin)
                    {
                        springBackVector.y = (CamPosMin.y + margin) - camPos.y;
                        currentCamScrollVelocity.y = 0;
                    }
                    else if (camPos.y > CamPosMax.y - margin)
                    {
                        springBackVector.y = (CamPosMax.y - margin) - camPos.y;
                        currentCamScrollVelocity.y = 0;
                    }
                    break;
                case CameraPlaneAxes.XZ_TOP_DOWN:
                    if (camPos.z < CamPosMin.y + margin)
                    {
                        springBackVector.z = (CamPosMin.y + margin) - camPos.z;
                        currentCamScrollVelocity.z = 0;
                    }
                    else if (camPos.z > CamPosMax.y - margin)
                    {
                        springBackVector.z = (CamPosMax.y - margin) - camPos.z;
                        currentCamScrollVelocity.z = 0;
                    }
                    break;
            }

            return springBackVector;
        }

        /// <summary>
        /// Internal helper method for setting the desired cam position.
        /// </summary>
        private void SetTargetPosition(Vector3 newPositionClamped)
        {
            targetPositionClamped = newPositionClamped;
        }

        /// <summary>
        /// Returns whether or not the camera is at the defined boundary.
        /// </summary>
        public bool GetIsBoundaryPosition(Vector3 testPosition)
        {

            bool isBoundaryPosition = false;
            switch (cameraAxes)
            {
                case CameraPlaneAxes.XY_2D_SIDESCROLL:
                    isBoundaryPosition = testPosition.x <= CamPosMin.x;
                    isBoundaryPosition |= testPosition.x >= CamPosMax.x;
                    isBoundaryPosition |= testPosition.y <= CamPosMin.y;
                    isBoundaryPosition |= testPosition.y >= CamPosMax.y;
                    break;
                case CameraPlaneAxes.XZ_TOP_DOWN:
                    isBoundaryPosition = testPosition.x <= CamPosMin.x;
                    isBoundaryPosition |= testPosition.x >= CamPosMax.x;
                    isBoundaryPosition |= testPosition.z <= CamPosMin.y;
                    isBoundaryPosition |= testPosition.z >= CamPosMax.y;
                    break;
            }
            return (isBoundaryPosition);
        }

        /// <summary>
        /// Returns a position that is clamped to the defined boundary.
        /// </summary>
        public Vector3 GetClampToBoundaries(Vector3 newPosition, bool includeSpringBackMargin = false)
        {

            float margin = 0;
            if (includeSpringBackMargin == true)
            {
                margin = camOverdragMargin;
            }

            switch (cameraAxes)
            {
                case CameraPlaneAxes.XY_2D_SIDESCROLL:
                    newPosition.x = Mathf.Clamp(newPosition.x, CamPosMin.x + margin, CamPosMax.x - margin);
                    newPosition.y = Mathf.Clamp(newPosition.y, CamPosMin.y + margin, CamPosMax.y - margin);
                    break;
                case CameraPlaneAxes.XZ_TOP_DOWN:
                    newPosition.x = Mathf.Clamp(newPosition.x, CamPosMin.x + margin, CamPosMax.x - margin);
                    newPosition.z = Mathf.Clamp(newPosition.z, CamPosMin.y + margin, CamPosMax.y - margin);
                    break;
            }
            return (newPosition);
        }

        /// <summary>
        /// Rotates a Vector2 by the given degrees.
        /// </summary>
        private Vector2 RotateVector2(Vector2 v, float degrees)
        {

            Vector2 vNormalized = v.normalized;
            float sin = Mathf.Sin(degrees * Mathf.Deg2Rad);
            float cos = Mathf.Cos(degrees * Mathf.Deg2Rad);

            float tx = vNormalized.x;
            float ty = vNormalized.y;
            vNormalized.x = (cos * tx) - (sin * ty);
            vNormalized.y = (sin * tx) + (cos * ty);
            return vNormalized * v.magnitude;
        }

        //计算摄像机四个角
        private void ComputeViewPos()
        {
            if(Cam.transform.position == oldPosition)
                return;
            oldPosition = Cam.transform.position;

            Spenve.Trapezium trapezium;
            trapezium.rightTop = GetIntersectionPoint(Cam.ScreenPointToRay(new Vector3(Screen.width, Screen.height, 0)));
            trapezium.leftTop = GetIntersectionPoint(Cam.ScreenPointToRay(new Vector3(0, Screen.height, 0)));
            trapezium.leftDown = GetIntersectionPoint(Cam.ScreenPointToRay(new Vector3(0, 0, 0)));
            trapezium.rightDown = GetIntersectionPoint(Cam.ScreenPointToRay(new Vector3(Screen.width, 0, 0)));
            trapezium.center = GetIntersectionPoint(Cam.ScreenPointToRay(new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0)));

            Spenve.MsgSystem.Instance.PostMessage("CameraViewPosChange", trapezium);
        }

        /// <summary>
        /// Method that computes the cam boundaries used for the current rotation and tilt of the camera.
        /// This computation is complex and needs to be invoked when the camera is rotated or tilted.
        /// </summary>
        private void ComputeCamBoundaries()
        {
            if (useBoundaryOrViewCenter)
            {
                float camRotation = GetRotationDeg();

                Vector2 camProjectedMin = Vector2.zero;
                Vector2 camProjectedMax = Vector2.zero;

                Vector2 camProjectedCenter = GetIntersection2d(new Ray(Transform.position, -RefPlane.normal)); //Get camera position projected vertically onto the ref plane. This allows to compute the offset that arises from camera tilt.

                //Fetch camera boundary as world-space coordinates projected to the ground.
                Vector2 camRight = GetIntersection2d(Cam.ScreenPointToRay(new Vector3(Screen.width, Screen.height * 0.5f, 0)));
                Vector2 camLeft = GetIntersection2d(Cam.ScreenPointToRay(new Vector3(0, Screen.height * 0.5f, 0)));
                Vector2 camUp = GetIntersection2d(Cam.ScreenPointToRay(new Vector3(Screen.width * 0.5f, Screen.height, 0)));
                Vector2 camDown = GetIntersection2d(Cam.ScreenPointToRay(new Vector3(Screen.width * 0.5f, 0, 0)));
                camProjectedMin = GetVector2Min(camRight, camLeft, camUp, camDown);
                camProjectedMax = GetVector2Max(camRight, camLeft, camUp, camDown);

                //Create rotated bounding box from boundaryMin/Max
                Vector2 computeBoundaryMin, computeBoundaryMax;
                RotateBoundingBox(boundaryMin, boundaryMax, -camRotation, out computeBoundaryMin, out computeBoundaryMax);

                Vector2 projectionCorrectionMin = new Vector2(camProjectedCenter.x - camProjectedMin.x, camProjectedCenter.y - camProjectedMin.y);
                Vector2 projectionCorrectionMax = new Vector2(camProjectedCenter.x - camProjectedMax.x, camProjectedCenter.y - camProjectedMax.y);

                CamPosMin = boundaryMin + projectionCorrectionMin;
                CamPosMax = boundaryMax + projectionCorrectionMax;

                if (CamPosMax.x - CamPosMin.x < camOverdragMargin * 2)
                {
                    float midPoint = (CamPosMax.x + CamPosMin.x) * 0.5f;
                    CamPosMax = new Vector2(midPoint + camOverdragMargin, CamPosMax.y);
                    CamPosMin = new Vector2(midPoint - camOverdragMargin, CamPosMin.y);
                }

                if (CamPosMax.y - CamPosMin.y < camOverdragMargin * 2)
                {
                    float midPoint = (CamPosMax.y + CamPosMin.y) * 0.5f;
                    CamPosMax = new Vector2(CamPosMax.x, midPoint + camOverdragMargin);
                    CamPosMin = new Vector2(CamPosMin.x, midPoint - camOverdragMargin);
                }
            }
            else
            {
                Vector2 camProjectedCenter = GetIntersection2d(new Ray(Transform.position, Transform.forward));
                Vector2 offset = new Vector2(camProjectedCenter.x - Transform.position.x, camProjectedCenter.y - Transform.position.z);
                CamPosMin = boundaryMin - offset;
                CamPosMax = boundaryMax - offset;
            }
        }

        /// <summary>
        /// Helper method for rotating a boundary box.
        /// </summary>
        private void RotateBoundingBox(Vector2 min, Vector2 max, float rotationDegrees, out Vector2 resultMin, out Vector2 resultMax)
        {
            Vector2 v0 = new Vector2(max.x, 0);
            Vector2 v1 = new Vector2(0, max.y);
            Vector2 v2 = new Vector2(min.x, 0);
            Vector2 v3 = new Vector2(0, min.y);
            Vector2 v0Rot = RotateVector2(v0, rotationDegrees);
            Vector2 v1Rot = RotateVector2(v1, rotationDegrees);
            Vector2 v2Rot = RotateVector2(v2, rotationDegrees);
            Vector2 v3Rot = RotateVector2(v3, rotationDegrees);
            resultMin = new Vector2(Mathf.Min(v0Rot.x, v1Rot.x, v2Rot.x, v3Rot.x), Mathf.Min(v0Rot.y, v1Rot.y, v2Rot.y, v3Rot.y));
            resultMax = new Vector2(Mathf.Max(v0Rot.x, v1Rot.x, v2Rot.x, v3Rot.x), Mathf.Max(v0Rot.y, v1Rot.y, v2Rot.y, v3Rot.y));
        }

        /// <summary>
        /// Method for retrieving the intersection of the given ray with the defined ground
        /// in 2d space.
        /// </summary>
        private Vector2 GetIntersection2d(Ray ray)
        {
            Vector3 intersection3d = GetIntersectionPoint(ray);
            Vector2 intersection2d = new Vector2(intersection3d.x, 0);
            switch (cameraAxes)
            {
                case CameraPlaneAxes.XY_2D_SIDESCROLL:
                    intersection2d.y = GetIntersectionPoint(ray).y;
                    break;
                case CameraPlaneAxes.XZ_TOP_DOWN:
                    intersection2d.y = GetIntersectionPoint(ray).z;
                    break;
            }
            return (intersection2d);
        }

        private Vector2 GetVector2Min(Vector2 v0, Vector2 v1, Vector2 v2, Vector2 v3)
        {
            return new Vector2(Mathf.Min(v0.x, v1.x, v2.x, v3.x), Mathf.Min(v0.y, v1.y, v2.y, v3.y));
        }

        private Vector2 GetVector2Max(Vector2 v0, Vector2 v1, Vector2 v2, Vector2 v3)
        {
            return new Vector2(Mathf.Max(v0.x, v1.x, v2.x, v3.x), Mathf.Max(v0.y, v1.y, v2.y, v3.y));
        }

        public void LateUpdate()
        {

            //Pinch.
            UpdatePinch(Time.deltaTime);

            //Translation.
            UpdatePosition(Time.deltaTime);

            #region editor codepath
#if UNITY_EDITOR
            //Allow to use the middle mouse wheel in editor to be able to zoom without touch device during development.
            float mouseScrollDelta = Input.GetAxis("Mouse ScrollWheel");
            bool isEditorInputRotate = false;
            bool isEditorInputTilt = false;
            bool anyModifierPressed = Input.GetKey(KeyCode.RightAlt) || Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
            if (anyModifierPressed == true)
            {
                if (Input.GetKeyDown(KeyCode.KeypadPlus))
                {
                    mouseScrollDelta = 0.05f;
                }
                else if (Input.GetKeyDown(KeyCode.KeypadMinus))
                {
                    mouseScrollDelta = -0.05f;
                }
                else if (Input.GetKeyDown(KeyCode.LeftArrow))
                {
                    mouseScrollDelta = 0.05f;
                    isEditorInputRotate = true;
                }
                else if (Input.GetKeyDown(KeyCode.RightArrow))
                {
                    mouseScrollDelta = -0.05f;
                    isEditorInputRotate = true;
                }
                else if (Input.GetKeyDown(KeyCode.UpArrow))
                {
                    mouseScrollDelta = 0.05f;
                    isEditorInputTilt = true;
                }
                else if (Input.GetKeyDown(KeyCode.DownArrow))
                {
                    mouseScrollDelta = -0.05f;
                    isEditorInputTilt = true;
                }
            }
            if (Mathf.Approximately(mouseScrollDelta, 0) == false)
            {
                if (isEditorInputRotate == true)
                {
                    if (EnableRotation == true)
                    {
                        Vector3 rotationAxis = GetRotationAxis();
                        Vector3 intersectionScreenCenter = GetIntersectionPoint(Cam.ScreenPointToRay(Input.mousePosition));
                        Transform.RotateAround(intersectionScreenCenter, rotationAxis, mouseScrollDelta * 100);
                        ComputeCamBoundaries();
                    }
                }
                else if (isEditorInputTilt == true)
                {
                    if (EnableTilt == true)
                    {
                        UpdateCameraTilt(mouseScrollDelta * 100);
                    }
                }
                else
                {
                    float editorZoomFactor = 15;
                    if (Cam.orthographic)
                    {
                        editorZoomFactor = 15;
                    }
                    else
                    {
                        if (IsTranslationZoom)
                        {
                            editorZoomFactor = 300;
                        }
                        else
                        {
                            editorZoomFactor = 100;
                        }
                    }
                    float zoomAmount = mouseScrollDelta * editorZoomFactor;
                    float camSizeDiff = DoEditorCameraZoom(zoomAmount);
                    Vector3 intersectionScreenCenter = GetIntersectionPoint(Cam.ScreenPointToRay(new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0)));
                    Vector3 pinchFocusVector = GetIntersectionPoint(Cam.ScreenPointToRay(Input.mousePosition)) - intersectionScreenCenter;
                    float multiplier = (1.0f / CamZoom * camSizeDiff);
                    Transform.position += pinchFocusVector * multiplier;
                }
            }
            for (int i = 0; i < 3; ++i)
            {
                if (Input.GetKeyDown((KeyCode)((int)KeyCode.Alpha1 + i)))
                {
                    StartCoroutine(ZoomToTargetValueCoroutine(Mathf.Lerp(CamZoomMin, CamZoomMax, (float)i / 2.0f)));
                }
            }
#endif
            #endregion

            //When the camera is zoomed in further than the defined normal value, it will snap back to normal using the code below.
            if (IsPinching == false && IsDragging == false)
            {
                float camZoomDeltaToNormal = 0;
                if (CamZoom > camZoomMax)
                {
                    camZoomDeltaToNormal = CamZoom - camZoomMax;
                }
                else if (CamZoom < camZoomMin)
                {
                    camZoomDeltaToNormal = CamZoom - camZoomMin;
                }

                if (Mathf.Approximately(camZoomDeltaToNormal, 0) == false)
                {
                    float cameraSizeCorrection = Mathf.Lerp(0, camZoomDeltaToNormal, zoomBackSpringFactor * Time.deltaTime);
                    if (Mathf.Abs(cameraSizeCorrection) > Mathf.Abs(camZoomDeltaToNormal))
                    {
                        cameraSizeCorrection = camZoomDeltaToNormal;
                    }
                    CamZoom -= cameraSizeCorrection;
                }
            }
        }

        /// <summary>
        /// Editor helper code.
        /// </summary>
        private float DoEditorCameraZoom(float amount)
        {
            float newCamZoom = CamZoom - amount;
            newCamZoom = Mathf.Clamp(newCamZoom, camZoomMin, camZoomMax);
            float camSizeDiff = CamZoom - newCamZoom;
            if (enableZoomTilt == true)
            {
                UpdateTiltForAutoTilt(newCamZoom);
            }
            CamZoom = newCamZoom;
            return (camSizeDiff);
        }

        public void FixedUpdate()
        {

            ScreenRatio = GetScreenRatio();

            if (cameraScrollVelocity.sqrMagnitude > float.Epsilon)
            {
                float timeSinceDragStop = Time.realtimeSinceStartup - timeRealDragStop;
                float dampFactor = Mathf.Clamp01(timeSinceDragStop * dampFactorTimeMultiplier);
                float camScrollVel = cameraScrollVelocity.magnitude;
                float camScrollVelRelative = camScrollVel / autoScrollVelocityMax;
                Vector3 camVelDamp = dampFactor * cameraScrollVelocity.normalized * autoScrollDamp * Time.fixedDeltaTime;
                camVelDamp *= EvaluateAutoScrollDampCurve(1.0f - camScrollVelRelative);
                if (camVelDamp.sqrMagnitude >= cameraScrollVelocity.sqrMagnitude)
                {
                    cameraScrollVelocity = Vector3.zero;
                }
                else
                {
                    cameraScrollVelocity -= camVelDamp;
                }
            }
        }

        /// <summary>
        /// Helper method used for auto scroll.
        /// </summary>
        private float EvaluateAutoScrollDampCurve(float t)
        {
            if (autoScrollDampCurve == null || autoScrollDampCurve.length == 0)
            {
                return (1);
            }
            return autoScrollDampCurve.Evaluate(t);
        }

        private void InputControllerOnFingerDown(Vector3 pos)
        {
            cameraScrollVelocity = Vector3.zero;
        }

        private void InputControllerOnFingerUp()
        {
            isDraggingSceneObject = false;
        }

        private Vector3 GetDragVector(Vector3 dragPosStart, Vector3 dragPosCurrent)
        {
            Vector3 intersectionDragStart = GetIntersectionPoint(Cam.ScreenPointToRay(dragPosStart));
            Vector3 intersectionDragCurrent = GetIntersectionPoint(Cam.ScreenPointToRay(dragPosCurrent));
            return (intersectionDragCurrent - intersectionDragStart);
        }

        /// <summary>
        /// Helper method that computes the suggested auto cam velocity from
        /// the last few frames of the user drag.
        /// </summary>
        private Vector3 GetVelocityFromMoveHistory()
        {
            Vector3 momentum = Vector3.zero;
            if (DragCameraMoveVector.Count > 0)
            {
                for (int i = 0; i < DragCameraMoveVector.Count; ++i)
                {
                    momentum += DragCameraMoveVector[i];
                }
                momentum /= DragCameraMoveVector.Count;
            }
            if (CameraAxes == CameraPlaneAxes.XZ_TOP_DOWN)
            {
                momentum.y = momentum.z;
                momentum.z = 0;
            }
            return (momentum);
        }

        private void InputControllerOnDragStart(Vector3 dragPosStart, bool isLongTap)
        {

            if (isDraggingSceneObject == false)
            {
                cameraScrollVelocity = Vector3.zero;
                dragStartCamPos = Transform.position;
                IsDragging = true;
                DragCameraMoveVector.Clear();
                SetTargetPosition(Transform.position);
            }
        }

        private void InputControllerOnDragUpdate(Vector3 dragPosStart, Vector3 dragPosCurrent, Vector3 correctionOffset)
        {
            if (isDraggingSceneObject == false)
            {
                Vector3 dragVector = GetDragVector(dragPosStart, dragPosCurrent + correctionOffset);
                Vector3 posNewClamped = GetClampToBoundaries(dragStartCamPos - dragVector);
                SetTargetPosition(posNewClamped);
            }
            else
            {
                IsDragging = false;
            }
        }

        private void InputControllerOnDragStop(Vector3 dragStopPos, Vector3 dragFinalMomentum)
        {
            if (isDraggingSceneObject == false)
            {
                cameraScrollVelocity = GetVelocityFromMoveHistory();
                if (cameraScrollVelocity.sqrMagnitude >= autoScrollVelocityMax * autoScrollVelocityMax)
                {
                    cameraScrollVelocity = cameraScrollVelocity.normalized * autoScrollVelocityMax;
                }
                timeRealDragStop = Time.realtimeSinceStartup;
                DragCameraMoveVector.Clear();
            }
            IsDragging = false;
        }

        private void InputControllerOnPinchStart(Vector3 pinchCenter, float pinchDistance)
        {

            pinchStartCamZoomSize = CamZoom;
            pinchStartIntersectionCenter = GetIntersectionPoint(Cam.ScreenPointToRay(pinchCenter));

            pinchCenterCurrent = pinchCenter;
            pinchDistanceCurrent = pinchDistance;
            pinchDistanceStart = pinchDistance;

            pinchCenterCurrentLerp = pinchCenter;
            pinchDistanceCurrentLerp = pinchDistance;

            SetTargetPosition(Transform.position);
            IsPinching = true;
            isRotationActivated = false;
            ResetPinchRotation(0);

            pinchTiltCurrent = 0;
            pinchTiltAccumulated = 0;
            pinchTiltLastFrame = 0;
            isTiltModeEvaluated = false;
            isPinchTiltMode = false;

            if (EnableTilt == false)
            {
                isTiltModeEvaluated = true; //Early out of this evaluation in case tilt is not enabled.
            }
        }

        private void InputControllerOnPinchUpdate(PinchUpdateData pinchUpdateData)
        {

            if (EnableTilt == true)
            {
                pinchTiltCurrent += pinchUpdateData.pinchTiltDelta;
                pinchTiltAccumulated += Mathf.Abs(pinchUpdateData.pinchTiltDelta);

                if (isTiltModeEvaluated == false && pinchUpdateData.pinchTotalFingerMovement > pinchModeDetectionMoveTreshold)
                {
                    isPinchTiltMode = Mathf.Abs(pinchTiltCurrent) > pinchTiltModeThreshold;
                    isTiltModeEvaluated = true;
                    if (isPinchTiltMode == true && isPinchModeExclusive == true)
                    {
                        pinchStartIntersectionCenter = GetIntersectionPoint(GetCamCenterRay());
                    }
                }
            }

            if (isTiltModeEvaluated == true)
            {
#pragma warning disable 162
                if (isPinchModeExclusive == true)
                {
                    pinchCenterCurrent = pinchUpdateData.pinchCenter;

                    if (isPinchTiltMode == true)
                    {

                        //Evaluate a potential break-out from a tilt. Under certain tweak-settings the tilt may trigger prematurely and needs to be overrided.
                        if (pinchTiltAccumulated < pinchAccumBreakout)
                        {
                            bool breakoutZoom = Mathf.Abs(pinchDistanceStart - pinchUpdateData.pinchDistance) > pinchDistanceForTiltBreakout;
                            bool breakoutRot = enableRotation == true && Mathf.Abs(pinchAngleCurrent) > rotationLockThreshold;
                            if (breakoutZoom == true || breakoutRot == true)
                            {
                                InputControllerOnPinchStart(pinchUpdateData.pinchCenter, pinchUpdateData.pinchDistance);
                                isTiltModeEvaluated = true;
                                isPinchTiltMode = false;
                            }
                        }
                    }
                }
#pragma warning restore 162
                pinchDistanceCurrent = pinchUpdateData.pinchDistance;

                if (enableRotation == true)
                {
                    if (Mathf.Abs(pinchUpdateData.pinchAngleDeltaNormalized) > rotationDetectionDeltaThreshold)
                    {
                        pinchAngleCurrent += pinchUpdateData.pinchAngleDelta;
                    }

                    if (pinchDistanceCurrent > rotationMinPinchDistance)
                    {
                        if (isRotationActivated == false)
                        {
                            ResetPinchRotation(0);
                            isRotationActivated = true;
                        }
                    }
                    else
                    {
                        isRotationActivated = false;
                    }
                }
            }
        }

        private void ResetPinchRotation(float currentPinchRotation)
        {
            pinchAngleCurrent = currentPinchRotation;
            pinchAngleCurrentLerp = currentPinchRotation;
            pinchAngleLastFrame = currentPinchRotation;
            isRotationLock = true;
        }

        private void InputControllerOnPinchStop()
        {
            IsPinching = false;
            DragCameraMoveVector.Clear();
            isPinchTiltMode = false;
            isTiltModeEvaluated = false;
        }

        private void InputControllerOnInputClick(Vector3 clickPosition, bool isDoubleClick, bool isLongTap)
        {

            if (isLongTap == true)
            {
                return;
            }

            Ray camRay = Cam.ScreenPointToRay(clickPosition);
            if (OnPickItem != null || OnPickItemDoubleClick != null)
            {
                RaycastHit hitInfo;
                if (Physics.Raycast(camRay, out hitInfo) == true)
                {
                    if (OnPickItem != null)
                    {
                        OnPickItem.Invoke(hitInfo);
                    }
                    if (isDoubleClick == true)
                    {
                        if (OnPickItemDoubleClick != null)
                        {
                            OnPickItemDoubleClick.Invoke(hitInfo);
                        }
                    }
                }
            }
            if (OnPickItem2D != null || OnPickItem2DDoubleClick != null)
            {
                RaycastHit2D hitInfo2D = Physics2D.Raycast(camRay.origin, camRay.direction);
                if (hitInfo2D == true)
                {
                    if (OnPickItem2D != null)
                    {
                        OnPickItem2D.Invoke(hitInfo2D);
                    }
                    if (isDoubleClick == true)
                    {
                        if (OnPickItem2DDoubleClick != null)
                        {
                            OnPickItem2DDoubleClick.Invoke(hitInfo2D);
                        }
                    }
                }
            }
        }

        public void OnDragSceneObject()
        {
            isDraggingSceneObject = true;
        }

        private float GetScreenRatio()
        {
            return ((float)Screen.width / (float)Screen.height);
        }

        private IEnumerator ZoomToTargetValueCoroutine(float target)
        {

            if (Mathf.Approximately(target, CamZoom) == false)
            {
                float startValue = CamZoom;
                const float duration = 0.3f;
                float timeStart = Time.time;
                while (Time.time < timeStart + duration)
                {
                    float progress = (Time.time - timeStart) / duration;
                    CamZoom = Mathf.Lerp(startValue, target, Mathf.Sin(-Mathf.PI * 0.5f + progress * Mathf.PI) * 0.5f + 0.5f);
                    yield return null;
                }
                CamZoom = target;
            }
        }

        public string CheckCameraAxesErrors()
        {
            string error = "";
            if (Transform.forward == Vector3.down && cameraAxes != CameraPlaneAxes.XZ_TOP_DOWN)
            {
                error = "Camera is pointing down but the cameraAxes is not set to TOP_DOWN. Make sure to set the cameraAxes variable properly.";
            }
            if (Transform.forward == Vector3.forward && cameraAxes != CameraPlaneAxes.XY_2D_SIDESCROLL)
            {
                error = "Camera is pointing sidewards but the cameraAxes is not set to 2D_SIDESCROLL. Make sure to set the cameraAxes variable properly.";
            }
            return (error);
        }

        private Ray GetCamCenterRay()
        {
            return (Cam.ScreenPointToRay(new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0)));
        }

        public void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Vector2 boundaryCenter2d = 0.5f * (boundaryMin + boundaryMax);
            Vector2 boundarySize2d = boundaryMax - boundaryMin;
            Vector3 boundaryCenter = UnprojectVector2(boundaryCenter2d, groundLevelOffset);
            Vector3 boundarySize = UnprojectVector2(boundarySize2d);
            Gizmos.DrawWireCube(boundaryCenter, boundarySize);
        }

        /// <summary>
        /// Helper method that unprojects the given Vector2 to a Vector3
        /// according to the camera axes setting.
        /// </summary>
        public Vector3 UnprojectVector2(Vector2 v2, float offset = 0)
        {
            if (CameraAxes == CameraPlaneAxes.XY_2D_SIDESCROLL)
            {
                return new Vector3(v2.x, v2.y, offset);
            }
            else
            {
                return new Vector3(v2.x, offset, v2.y);
            }
        }

        public Vector2 ProjectVector3(Vector3 v3)
        {
            if (CameraAxes == CameraPlaneAxes.XY_2D_SIDESCROLL)
            {
                return new Vector2(v3.x, v3.y);
            }
            else
            {
                return new Vector2(v3.x, v3.z);
            }
        }
    }
}
