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
			Debug.Log($"Connected to server: {server}");
		};

		netManager = new NetManager(netListener);
		netManager.Start();
		netManager.Connect("localhost", 9050, "DeathRoomSecret");
	}

	void FixedUpdate() {
		netManager.PollEvents();
	}
}
