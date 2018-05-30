using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UI;

public class PluginTest : MonoBehaviour
{

#if UNITY_IOS

	private delegate void intCallback(int result);

	[DllImport("__Internal")]
	private static extern double IOSgetElapsedTime();

	[DllImport("__Internal")]
	private static extern void IOScreateNativeAlert(string[] strings, int stringCount, intCallback callback);

	[DllImport("__Internal")]
	private static extern void IOSshareScreenImage(byte[] imagePNG, long imageLen, string caption, intCallback callback);

	[DllImport("__Internal")]
	private static extern void IOSshowWebView(string URL, int pixelSpace);

	[DllImport("__Internal")]
	private static extern void IOShideWebView(intCallback callback);


#endif

	public Button shareButton;

	public RectTransform webPanel;
	public RectTransform buttonStrip;

	// Use this for initialization
	void Start()
	{

		Debug.Log("Elapsed Time: " + getElapsedTime());
		//StartCoroutine(ShowDialog(Random.Range(7,12)));
	}

	IEnumerator ShowDialog(float delayTime)
	{
		Debug.Log("Will show alert after " + delayTime + " seconds");
		if (delayTime > 0)
			yield return new WaitForSeconds(delayTime);
		CreateIOSAlert(new string[] { "Title", "Message", "DefaultButton", "OtherButton" });
	}


	double getElapsedTime()
	{
		if (Application.platform == RuntimePlatform.IPhonePlayer)
			return IOSgetElapsedTime();
		Debug.LogWarning("Wrong platform!");
		return 0;
	}

	[AOT.MonoPInvokeCallback(typeof(intCallback))]
	static void nativeAlertHandler(int result)
	{
		Debug.Log("Unity: clicked button at index: " + result);
	}

	public void CreateIOSAlert(string[] strings)
	{
		if (strings.Length < 3)
		{
			Debug.LogError("Alert requires at least 3 strings!");
			return;
		}

		if (Application.platform == RuntimePlatform.IPhonePlayer)
			IOScreateNativeAlert(strings, strings.Length, nativeAlertHandler);
		else
			Debug.LogWarning("Can only display alert on iOS");
		Debug.Log("Alert shown after: " + getElapsedTime() + " seconds");
	}

	public void ShareScreenTapped()
	{
		if (shareButton != null)
			shareButton.gameObject.SetActive(false);
		ShareScreenShot(Application.productName + " screenshot", (int result) =>
		{
			Debug.Log("Share completed with: " + result);
			CreateIOSAlert(new string[] { "Share Complete", "Share completed with: " + result, "OK" });
			if (shareButton != null)
				shareButton.gameObject.SetActive(true);
		});

	}

	static System.Action<int> ShareCompleteAction;

	static bool isSharingScreenShot;

	[AOT.MonoPInvokeCallback(typeof(intCallback))]
	static void shareCallback(int result)
	{
		Debug.Log("Unity: share completed with: " + result);
		if (ShareCompleteAction != null)
			ShareCompleteAction(result);
		isSharingScreenShot = false;
	}

	public void ShareScreenShot(string caption, System.Action<int> shareComplete)
	{
		if (isSharingScreenShot)
		{
			Debug.LogError("already sharing screenshot - aborting");
			return;
		}
		isSharingScreenShot = true;
		ShareCompleteAction = shareComplete;
		StartCoroutine(waitForEndOfFrame(caption));
	}

	IEnumerator waitForEndOfFrame(string caption)
	{
		yield return new WaitForEndOfFrame();
		Texture2D image = ScreenCapture.CaptureScreenshotAsTexture();
		Debug.Log("Image size: " + image.width + " x " + image.height);
		byte[] imagePNG = image.EncodeToPNG();
		Debug.Log("PNG size: " + imagePNG.Length);
		if (Application.platform == RuntimePlatform.IPhonePlayer)
			IOSshareScreenImage(imagePNG, imagePNG.Length, caption, shareCallback);
		Object.Destroy(image);
	}

	public void OpenWebView(string url, int pixelShift)
	{
		if (Application.platform == RuntimePlatform.IPhonePlayer)
		{
			IOSshowWebView(url, pixelShift);
		}
	}

	public void CloseWebView(System.Action<int> closeComplete)
	{
		onCloseWebView = closeComplete;
		if (Application.platform == RuntimePlatform.IPhonePlayer)
		{
			IOShideWebView(closeWebViewHandler);
		}
		else
			closeWebViewHandler(0);
	}

	[AOT.MonoPInvokeCallback(typeof(intCallback))]
	static void closeWebViewHandler(int result)
	{
		if (onCloseWebView != null)
			onCloseWebView(result);
		onCloseWebView = null;
	}
	static System.Action<int> onCloseWebView;



	public void OpenWebViewTapped()
	{
		Canvas parentCanvas = buttonStrip.GetComponentInParent<Canvas>();
		int stripHeight = (int)(buttonStrip.rect.height * parentCanvas.scaleFactor + 0.5f);
		webPanel.gameObject.SetActive(true);
		OpenWebView("http://www.cwgtech.com", stripHeight);
	}

	public void CloseWebViewTapped()
	{
		CloseWebView((int result) =>
		{
			webPanel.gameObject.SetActive(false);
		});
	}
}
