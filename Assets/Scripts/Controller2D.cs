using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class RaycastHit2DEvent : UnityEvent<RaycastHit2D> {}

public class Controller2D : RaycastController {

	public float maxSlopeAngle = 80f;
	public CollisionInfo collisions;

	public RaycastHit2DEvent onTriggerHit;
	public RaycastHit2DEvent onColliderHit;

	//
	// Strucs
	//

	public struct CollisionInfo {
		public bool above, bellow;
		public bool left, right;
		public bool climbingSlope;
		public bool descendingSlope;

		public bool slidingDownMaxSlope;
		public float slopeAngle, slopeAngleOld;
		public Vector2 moveAmountOld;
		public int facedDir;
		public bool fallingThroughPlatform;
		public Vector2 slopeNormal;

		public void Reset() {
			above = bellow = false;
			left = right = false;
			climbingSlope = false;
			descendingSlope = false;
			slidingDownMaxSlope = false;
			fallingThroughPlatform = false;
			slopeNormal = Vector2.zero;
			slopeAngleOld = slopeAngle;
			slopeAngle = 0;
		}
	}

	private Vector2 playerInput;

	//
	// LifeCycles
	//

	protected override void Awake() {
		this.onTriggerHit = new RaycastHit2DEvent();
		this.onColliderHit = new RaycastHit2DEvent();
		base.Awake();
		this.collisions.facedDir = 1;
	}

	private void OnDisable() {
		this.onTriggerHit.RemoveAllListeners();
		this.onColliderHit.RemoveAllListeners();
	}

	//
	// Public
	//

	public void subscribeToTriggerEvent(UnityAction<RaycastHit2D> callback) {
		this.onTriggerHit.AddListener(callback);
	}

	public void subscribeToColliderEvent(UnityAction<RaycastHit2D> callback) {
		this.onColliderHit.AddListener(callback);
	}

	public void Move(Vector2 moveAmount, bool standingOnPlatform = false) {
		Move(moveAmount, Vector2.zero, standingOnPlatform);
	}

	public void Move(Vector2 moveAmount, Vector2 input, bool standingOnPlatform = false) {
		UpdateRaycastOrigins();
		this.collisions.Reset();
		this.collisions.moveAmountOld = moveAmount;
		this.playerInput = input;

		if (moveAmount.y < 0) {
			DescendSlope(ref moveAmount);
		}

		if (moveAmount.x != 0) {
			this.collisions.facedDir = (int)Mathf.Sign(moveAmount.x);
		}

		HorizontalCollisions(ref moveAmount);
		if (moveAmount.y != 0) {
			VerticalCollisions(ref moveAmount);
		}

		transform.Translate(moveAmount);

		if (standingOnPlatform) {
			this.collisions.bellow = true;
		}
	}


	//
	// Private
	//

	private void HorizontalCollisions(ref Vector2 moveAmount) {
		float directionX = this.collisions.facedDir;
		float rayLength = Mathf.Abs(moveAmount.x) + this.skinWidth;

		if (Mathf.Abs(moveAmount.x) < skinWidth) {
			rayLength = 2 * this.skinWidth;
		}

		for (int i = 0; i < this.horizontalRayCount; i++) {
			Vector2 rayOrigin = (directionX == -1) ? this.raycastOrigins.bottomLeft : this.raycastOrigins.bottomRight;
			rayOrigin += Vector2.up * (this.horizontalRaySpacing * i);
			RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.right * directionX, rayLength, this.hitMask);

			Debug.DrawRay(rayOrigin, Vector2.right * directionX * rayLength, Color.red);

			if (hit) {

				if (this.triggerMask == (this.triggerMask | (1 << hit.collider.gameObject.layer))) {
					this.onTriggerHit.Invoke(hit);
				} else {
					this.onColliderHit.Invoke(hit);

					if (hit.distance == 0 || hit.collider.tag == "OneWay") continue;

					float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);
					if (i == 0 && slopeAngle <= maxSlopeAngle) {
						if (this.collisions.descendingSlope) {
							this.collisions.descendingSlope = false;
							moveAmount = this.collisions.moveAmountOld;
						}
						float distanceToSlopeStart = 0;
						if (slopeAngle != this.collisions.slopeAngleOld) {
							distanceToSlopeStart = hit.distance - this.skinWidth;
							moveAmount.x -= distanceToSlopeStart * directionX;
						}
						ClimbSlope(ref moveAmount, slopeAngle, hit.normal);
						moveAmount.x += distanceToSlopeStart * directionX;
					}

					if (!this.collisions.climbingSlope || slopeAngle > this.maxSlopeAngle) {
						moveAmount.x = (hit.distance - this.skinWidth) * directionX;
						rayLength = hit.distance;

						if (this.collisions.climbingSlope) {
							moveAmount.y = Mathf.Tan(this.collisions.slopeAngle * Mathf.Deg2Rad) * Mathf.Abs(moveAmount.x);
						}

						collisions.left = directionX == -1;
						collisions.right = directionX == 1;
					}
				}
			}
		}
	}

	private void VerticalCollisions(ref Vector2 moveAmount) {
		float directionY = Mathf.Sign(moveAmount.y);
		float rayLength = Mathf.Abs(moveAmount.y) + this.skinWidth;

		for (int i = 0; i < this.verticalRayCount; i++) {
			Vector2 rayOrigin = (directionY == -1) ? this.raycastOrigins.bottomLeft : this.raycastOrigins.topLeft;
			rayOrigin += Vector2.right * (this.verticalRaySpacing * i + moveAmount.x);
			RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.up * directionY, rayLength, this.hitMask);

			Debug.DrawRay(rayOrigin, Vector2.up * directionY * rayLength, Color.red);

			if (hit) {

				if (this.triggerMask == (this.triggerMask | (1 << hit.collider.gameObject.layer))) {
					this.onTriggerHit.Invoke(hit);
				} else {
					this.onColliderHit.Invoke(hit);

					if (hit.collider.tag == "OneWay") {
						if (directionY == 1 || hit.distance == 0) {
							continue;
						}
						if (this.collisions.fallingThroughPlatform) {
							continue;
						}
						if (playerInput.y == -1) {
							this.collisions.fallingThroughPlatform = true;
							Invoke("ResetFallingThroughPlatform", .5f);
							continue;
						}
					}

					moveAmount.y = (hit.distance - this.skinWidth) * directionY;
					rayLength = hit.distance;

					if (this.collisions.climbingSlope) {
						moveAmount.x = moveAmount.y / Mathf.Tan(this.collisions.slopeAngle * Mathf.Deg2Rad) * Mathf.Sign(moveAmount.y);
					}

					collisions.bellow = directionY == -1;
					collisions.above = directionY == 1;
				}
			}
		}

		if (this.collisions.climbingSlope) {
			float directionX = Mathf.Sign(moveAmount.x);
			rayLength = Mathf.Abs(moveAmount.x) + skinWidth;
			Vector2 rayOrigin = ((directionX == -1) ? this.raycastOrigins.bottomLeft : this.raycastOrigins.bottomRight) + Vector2.up * moveAmount.y;
			RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.right * directionX, rayLength, this.hitMask);

			if (hit) {
				float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);
				if (slopeAngle != this.collisions.slopeAngle) {
					moveAmount.x = (hit.distance - this.skinWidth) * directionX;
					this.collisions.slopeAngle = slopeAngle;
					this.collisions.slopeNormal = hit.normal;
				}
			}
		}
	}

	private void ClimbSlope(ref Vector2 moveAmount, float slopeAngle, Vector2 slopeNormal) {
		float moveDistance = Mathf.Abs(moveAmount.x);
		float climbmoveAmountY = Mathf.Sin(slopeAngle * Mathf.Deg2Rad) * moveDistance;

		if (moveAmount.y <= climbmoveAmountY) {
			moveAmount.y = climbmoveAmountY;
			moveAmount.x = Mathf.Cos(slopeAngle * Mathf.Deg2Rad) * moveDistance * Mathf.Sign(moveAmount.x);
			this.collisions.bellow = true;
			this.collisions.climbingSlope = true;
			this.collisions.slopeAngle = slopeAngle;
			this.collisions.slopeNormal = slopeNormal;
		}
	}

	private void DescendSlope(ref Vector2 moveAmount) {

		RaycastHit2D maxSlopeHitLeft = Physics2D.Raycast(this.raycastOrigins.bottomLeft, Vector2.down, Mathf.Abs(moveAmount.y) + this.skinWidth, this.hitMask);
		RaycastHit2D maxSlopeHitRight = Physics2D.Raycast(this.raycastOrigins.bottomRight, Vector2.down, Mathf.Abs(moveAmount.y) + this.skinWidth, this.hitMask);

		if (maxSlopeHitLeft ^ maxSlopeHitRight) {
			SlideDownMaxSlope(maxSlopeHitLeft, ref moveAmount);
			SlideDownMaxSlope(maxSlopeHitRight, ref moveAmount);
		}

		if (!this.collisions.slidingDownMaxSlope) {
			float directionX = Mathf.Sign(moveAmount.x);
			Vector2 rayOrigin = (directionX == -1) ? this.raycastOrigins.bottomRight : this.raycastOrigins.bottomLeft;
			RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.down, Mathf.Infinity, this.hitMask);

			if (hit) {

				if (this.triggerMask == (this.triggerMask | (1 << hit.collider.gameObject.layer))) {
					this.onTriggerHit.Invoke(hit);
				} else {
					this.onColliderHit.Invoke(hit);

					float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);
					if (slopeAngle != 0 && slopeAngle <= this.maxSlopeAngle) {
						if (Mathf.Sign(hit.normal.x) == directionX) {
							if (hit.distance - skinWidth <= Mathf.Tan(slopeAngle * Mathf.Deg2Rad) * Mathf.Abs(moveAmount.x)) {
								float moveDistance = Mathf.Abs(moveAmount.x);
								float descendMoveAmountY = Mathf.Sin(slopeAngle * Mathf.Deg2Rad) * moveDistance;
								moveAmount.x = Mathf.Cos(slopeAngle * Mathf.Deg2Rad) * moveDistance * Mathf.Sign(moveAmount.x);
								moveAmount.y -= descendMoveAmountY;

								this.collisions.slopeAngle = slopeAngle;
								this.collisions.descendingSlope = true;
								this.collisions.bellow = true;
								this.collisions.slopeNormal = hit.normal;
							}
						}
					}
				}
			}
		}
	}

	void SlideDownMaxSlope(RaycastHit2D hit, ref Vector2 moveAmount) {
		if (hit) {
			float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);
			if (slopeAngle > this.maxSlopeAngle) {
				moveAmount.x = Mathf.Sign(hit.normal.x) *  (Mathf.Abs(moveAmount.y) - hit.distance) / Mathf.Tan(slopeAngle * Mathf.Deg2Rad);

				this.collisions.slopeAngle = slopeAngle;
				this.collisions.slidingDownMaxSlope = true;
				this.collisions.slopeNormal = hit.normal;
			}
		}
	}

	private void ResetFallingThroughPlatform() {
		this.collisions.fallingThroughPlatform = false;
	}
}
