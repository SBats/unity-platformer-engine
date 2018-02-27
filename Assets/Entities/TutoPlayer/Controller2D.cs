using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Controller2D : RaycastController {

	public float maxClimbAngle = 80;
	public float maxDescendAngle = 80;
	public CollisionInfo collisions;

	//
	// Strucs
	//

	public struct CollisionInfo {
		public bool above, bellow;
		public bool left, right;
		public bool climbingSlope;
		public bool descendingSlope;
		public float slopeAngle, slopeAngleOld;
		public Vector3 velocityOld;
		public int facedDir;
		public bool fallingThroughPlatform;

		public void Reset() {
			above = bellow = false;
			left = right = false;
			climbingSlope = false;
			descendingSlope = false;
			fallingThroughPlatform = false;
			slopeAngleOld = slopeAngle;
			slopeAngle = 0;
		}
	}

	private Vector2 playerInput;

	//
	// LifeCycles
	//

	public override void Start() {
		base.Start();
		this.collisions.facedDir = 1;
	}

	//
	// Public
	//

	public void Move(Vector3 velocity, bool standingOnPlatform = false) {
		Move(velocity, Vector2.zero, standingOnPlatform);
	}

	public void Move(Vector3 velocity, Vector2 input, bool standingOnPlatform = false) {
		UpdateRaycastOrigins();
		this.collisions.Reset();
		this.collisions.velocityOld = velocity;
		this.playerInput = input;

		if (velocity.x != 0) {
			this.collisions.facedDir = (int)Mathf.Sign(velocity.x);
		}

		if (velocity.y < 0) {
			DescendSlope(ref velocity);
		}
		HorizontalCollisions(ref velocity);
		if (velocity.y != 0) {
			VerticalCollisions(ref velocity);
		}

		transform.Translate(velocity);

		if (standingOnPlatform) {
			this.collisions.bellow = true;
		}
	}


	//
	// Private
	//

	private void HorizontalCollisions(ref Vector3 velocity) {
		float directionX = this.collisions.facedDir;
		float rayLength = Mathf.Abs(velocity.x) + this.skinWidth;

		if (Mathf.Abs(velocity.x) < skinWidth) {
			rayLength = 2 * this.skinWidth;
		}

		for (int i = 0; i < this.horizontalRayCount; i++) {
			Vector2 rayOrigin = (directionX == -1) ? this.raycastOrigins.bottomLeft : this.raycastOrigins.bottomRight;
			rayOrigin += Vector2.up * (this.horizontalRaySpacing * i);
			RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.right * directionX, rayLength, this.collisionMask);

			Debug.DrawRay(rayOrigin, Vector2.right * directionX * rayLength, Color.red);

			if (hit) {

				if (hit.distance == 0) continue;

				float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);
				if (i == 0 && slopeAngle <= maxClimbAngle) {
					if (this.collisions.descendingSlope) {
						this.collisions.descendingSlope = false;
						velocity = this.collisions.velocityOld;
					}
					float distanceToSlopeStart = 0;
					if (slopeAngle != this.collisions.slopeAngleOld) {
						distanceToSlopeStart = hit.distance - this.skinWidth;
						velocity.x -= distanceToSlopeStart * directionX;
					}
					ClimbSlope(ref velocity, slopeAngle);
					velocity.x += distanceToSlopeStart * directionX;
				}

				if (!this.collisions.climbingSlope || slopeAngle > this.maxClimbAngle) {
					velocity.x = (hit.distance - this.skinWidth) * directionX;
					rayLength = hit.distance;

					if (this.collisions.climbingSlope) {
						velocity.y = Mathf.Tan(this.collisions.slopeAngle * Mathf.Deg2Rad) * Mathf.Abs(velocity.x);
					}

					collisions.left = directionX == -1;
					collisions.right = directionX == 1;
				}
			}
		}
	}

	private void VerticalCollisions(ref Vector3 velocity) {
		float directionY = Mathf.Sign(velocity.y);
		float rayLength = Mathf.Abs(velocity.y) + this.skinWidth;

		for (int i = 0; i < this.verticalRayCount; i++) {
			Vector2 rayOrigin = (directionY == -1) ? this.raycastOrigins.bottomLeft : this.raycastOrigins.topLeft;
			rayOrigin += Vector2.right * (this.verticalRaySpacing * i + velocity.x);
			RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.up * directionY, rayLength, this.collisionMask);

			Debug.DrawRay(rayOrigin, Vector2.up * directionY * rayLength, Color.red);

			if (hit) {

				if (hit.collider.tag == "Through") {
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

				velocity.y = (hit.distance - this.skinWidth) * directionY;
				rayLength = hit.distance;

				if (this.collisions.climbingSlope) {
					velocity.x = velocity.y / Mathf.Tan(this.collisions.slopeAngle * Mathf.Deg2Rad) * Mathf.Sign(velocity.y);
				}

				collisions.bellow = directionY == -1;
				collisions.above = directionY == 1;
			}
		}

		if (this.collisions.climbingSlope) {
			float directionX = Mathf.Sign(velocity.x);
			rayLength = Mathf.Abs(velocity.x) + skinWidth;
			Vector2 rayOrigin = ((directionX == -1) ? this.raycastOrigins.bottomLeft : this.raycastOrigins.bottomRight) + Vector2.up * velocity.y;
			RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.right * directionX, rayLength, this.collisionMask);

			if (hit) {
				float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);
				if (slopeAngle != this.collisions.slopeAngle) {
					velocity.x = (hit.distance - this.skinWidth) * directionX;
					this.collisions.slopeAngle = slopeAngle;
				}
			}
		}
	}

	private void ClimbSlope(ref Vector3 velocity, float slopeAngle) {
		float moveDistance = Mathf.Abs(velocity.x);
		float climbVelocityY = Mathf.Sin(slopeAngle * Mathf.Deg2Rad) * moveDistance;

		if (velocity.y <= climbVelocityY) {
			velocity.y = climbVelocityY;
			velocity.x = Mathf.Cos(slopeAngle * Mathf.Deg2Rad) * moveDistance * Mathf.Sign(velocity.x);
			this.collisions.bellow = true;
			this.collisions.climbingSlope = true;
			this.collisions.slopeAngle = slopeAngle;
		}
	}

	private void DescendSlope(ref Vector3 velocity) {
		float directionX = Mathf.Sign(velocity.x);
		Vector2 rayOrigin = (directionX == -1) ? this.raycastOrigins.bottomRight : this.raycastOrigins.bottomLeft;
		RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.down, Mathf.Infinity, this.collisionMask);

		if (hit) {
			float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);
			if (slopeAngle != 0 && slopeAngle <= this.maxDescendAngle) {
				if (Mathf.Sign(hit.normal.x) == directionX) {
					if (hit.distance - skinWidth <= Mathf.Tan(slopeAngle * Mathf.Deg2Rad) * Mathf.Abs(velocity.x)) {
						float moveDistance = Mathf.Abs(velocity.x);
						float descendVelocityY = Mathf.Sin(slopeAngle * Mathf.Deg2Rad) * moveDistance;
						velocity.x = Mathf.Cos(slopeAngle * Mathf.Deg2Rad) * moveDistance * Mathf.Sign(velocity.x);
						velocity.y -= descendVelocityY;

						this.collisions.bellow = true;
						this.collisions.descendingSlope = true;
						this.collisions.slopeAngle = slopeAngle;
					}
				}
			}
		}
	}

	private void ResetFallingThroughPlatform() {
		this.collisions.fallingThroughPlatform = false;
	}
}
