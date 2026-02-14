using UnityEngine;
using System.Net;
using System.Net.Sockets;

public class LobbyHostLAN : MonoBehaviour
{
	private TweenPosition _windowTween;

	private TweenAlpha _alphaTween;

	public UILabel ipAddress;

	public UIInput port;

	protected void Awake()
	{
		_windowTween = GetComponent<TweenPosition>();
		_alphaTween = GetComponent<TweenAlpha>();
	}

	private void Start()
	{
		if (LobbyController.Instance != null)
		{
			LobbyController.Instance.OnCloseHostLAN += Deactivate;
		}
		base.gameObject.SetActive(value: false);
	}

	private void Update()
	{
		if (InputManagerController.IsStartKeyDown())
		{
			HostLobby();
		}
	}

	private string GetLocalIPAddress()
	{
		try
		{
			foreach (System.Net.NetworkInformation.NetworkInterface ni in System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces())
			{
				if (ni.OperationalStatus == System.Net.NetworkInformation.OperationalStatus.Up && ni.NetworkInterfaceType != System.Net.NetworkInformation.NetworkInterfaceType.Loopback)
				{
					foreach (System.Net.NetworkInformation.UnicastIPAddressInformation ip in ni.GetIPProperties().UnicastAddresses)
					{
						if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
						{
							return ip.Address.ToString();
						}
					}
				}
			}
		}
		catch
		{
		}
		
		return "127.0.0.1";
	}

	public void Activate()
	{
		base.gameObject.SetActive(value: true);
		UICamera.selectedObject = port.gameObject;
		LobbyController.Instance.CurrentSubMenu = LobbyController.SubMenu.HostLAN;
		ipAddress.text = GetLocalIPAddress();
		port.value = "2500";
		_windowTween.ResetToBeginning();
		_alphaTween.ResetToBeginning();
		_windowTween.PlayForward();
		_alphaTween.PlayForward();
		BlackDefocus.In(GetComponent<UIWidget>().depth, -1f, -1f);
	}

	public void Deactivate()
	{
		OnlineMenu.Instance.SetOnlineMenuEnabled(isEnabled: true);
		LobbyController.Instance.CurrentSubMenu = LobbyController.SubMenu.OnlineMenu;
		OnlineMenu.Instance.hostLANButton.GetComponent<LobbyOptionButton>().SetSelected();
		BlackDefocus.Out();
		base.gameObject.SetActive(value: false);
	}

	public void HostLobby()
	{
		AudioManager.ClickSound();
		Deactivate();
		NetworkManager.UsePhoton = false;
		OnlineMenu.Instance.CreateRoom(string.Empty, friendsOnly: false, port.value);
	}
}
