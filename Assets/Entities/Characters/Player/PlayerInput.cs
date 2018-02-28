using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent (typeof (PlayerController))]
public class PlayerInput : MonoBehaviour {

	private PlayerController player;

	// Use this for initialization
	void Start () {
		player = GetComponent<PlayerController>();
	}

	// Update is called once per frame
	void Update () {
		Vector2 directionalInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
		this.player.SetDirectionalInput(directionalInput);

		if (Input.GetKeyDown(KeyCode.Space)) {
			this.player.OnJumpInputDown();
		}

		if (Input.GetKeyUp(KeyCode.Space)) {
			this.player.OnJumpInputUp();
		}
	}
}
