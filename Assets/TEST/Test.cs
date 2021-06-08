using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;


public class Test : MonoBehaviour
{
	private void Awake()
	{
		A();
		cts.Cancel();
	}


	CancellationTokenSource cts = new CancellationTokenSource();
	void A()
	{
		var t = cts.Token;
		t.RegisterWithoutCaptureExecutionContext(() => print("ok"));
	}
}
