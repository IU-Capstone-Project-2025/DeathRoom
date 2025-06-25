using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LiteNetLib;

public class Client : MonoBehaviour {
	NetManager netManager;
	EventBasedNetListener netListener;

	void Start() {
		netListener = new EventBasedNetListener();
		netListener.PeerConnectedEvent += (server) => {
			Debug.LogError($"Connected to server: {server}");
		};

		netManager = new NetManager(netListener);
		netManager.Start();
		// netManager.Connect("10.91.57.163", 9050);
	}

	void FixedUpdate() {
		netManager.PollEvents();
	}
}
