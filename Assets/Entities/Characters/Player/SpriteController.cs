using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpriteController : MonoBehaviour {

	public struct PlayerState {
		public Controller2D.CollisionInfo collisions;
		public bool grounded;
		public Vector2 velocity;
		public bool wallSliding;
		public Vector2 input;
	}

	private Animator animator;
	private SpriteRenderer spriteRenderer;
	private PlayerState oldPlayerState;

	private void Awake() {
		this.animator = GetComponent<Animator>();
		this.spriteRenderer = GetComponent<SpriteRenderer>();
	}

	public void updateSprite(PlayerState playerState) {
		if (playerState.collisions.bellow) {
			if (playerState.input.x != 0) {
				this.spriteRenderer.flipX = Mathf.Sign(playerState.input.x) < 0;
			}
		} else if (playerState.wallSliding) {
			this.spriteRenderer.flipX = playerState.collisions.right;
		} else {
			this.spriteRenderer.flipX = Mathf.Sign(playerState.velocity.x) < 0;
		}

		updateAnimatorParameters(playerState);
		this.oldPlayerState = playerState;
	}

	private void updateAnimatorParameters (PlayerState playerState) {
		this.animator.SetBool("grounded", playerState.collisions.bellow);
		this.animator.SetBool("wall_sliding", playerState.wallSliding);
		this.animator.SetFloat("x_velocity", playerState.velocity.x);
		this.animator.SetFloat("y_velocity", playerState.velocity.y);
		this.animator.SetFloat("absolute_x_velocity", Mathf.Abs(playerState.velocity.x));
	}
}
