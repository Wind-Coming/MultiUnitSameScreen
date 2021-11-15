using UnityEngine;
using System.Collections;

public class ReporterGUI : MonoBehaviour
{
#if !REPORTER_DISABLE

	Reporter reporter;
	void Awake()
	{
		reporter = gameObject.GetComponent<Reporter>();
	}

	void OnGUI()
	{
		reporter.OnGUIDraw();
	}
#endif
}
