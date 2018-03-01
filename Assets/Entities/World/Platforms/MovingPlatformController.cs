using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovingPlatformController : RaycastController {

	public LayerMask passengerMask;
	public float speed;
	public bool cyclic;
	public float waitTime;
	[Range(1,3)]
	public float easeAmount = 1f;
	public Vector3[] localWaypoints;

	private Vector3[] globalWaypoints;
	private int fromWaypointIndex;
	private float percentBetweenWaypoints;
	private float nextMoveTime;
	private List<PassengerMovement> passengerMovement;
	private Dictionary<Transform, Controller2D> passengerDictionary = new Dictionary<Transform, Controller2D>();

	private struct PassengerMovement {
		public Transform transform;
		public Vector3 velocity;
		public bool standingOnPlatform;
		public bool moveBeforePlatform;

		public PassengerMovement(Transform _transform, Vector3 _velocity, bool _standingOnPlatform, bool _moveBeforePlatform) {
			transform = _transform;
			velocity = _velocity;
			standingOnPlatform = _standingOnPlatform;
			moveBeforePlatform = _moveBeforePlatform;
		}
	}

	//
	// Lifecycles
	//

	protected override void Awake() {
		base.Awake();

		this.globalWaypoints = new Vector3[this.localWaypoints.Length];
		for (int i = 0; i < this.localWaypoints.Length; i++) {
			this.globalWaypoints[i] = localWaypoints[i] + transform.position;
		}
	}

	private void Update() {
		UpdateRaycastOrigins();

		Vector3 velocity = CalculatePassengerMovement();

		CalculatePassengerMovement(velocity);

		MovePassengers(true);
		transform.Translate(velocity);
		MovePassengers(false);
	}

	//
	// Private
	//

	private float Ease(float x) {
		float a = easeAmount;
		return Mathf.Pow(x, a) / (Mathf.Pow(x, a) + Mathf.Pow(1 - x, a));
	}

	private Vector3 CalculatePassengerMovement() {

		if (Time.time < this.nextMoveTime) {
			return Vector3.zero;
		}

		this.fromWaypointIndex %= this.globalWaypoints.Length;
		int toWaypointIndex = (this.fromWaypointIndex + 1) % this.globalWaypoints.Length;
		float distanceBetweenWaypoints = Vector3.Distance(this.globalWaypoints[this.fromWaypointIndex], this.globalWaypoints[toWaypointIndex]);

		this.percentBetweenWaypoints += Time.deltaTime * speed / distanceBetweenWaypoints;
		this.percentBetweenWaypoints = Mathf.Clamp01(this.percentBetweenWaypoints);
		float easedPercentBetweenWaypoints = Ease(this.percentBetweenWaypoints);

		Vector3 newPos = Vector3.Lerp(this.globalWaypoints[this.fromWaypointIndex], this.globalWaypoints[toWaypointIndex], easedPercentBetweenWaypoints);

		if (percentBetweenWaypoints >= 1) {
			this.percentBetweenWaypoints = 0;
			this.fromWaypointIndex++;

			if (!this.cyclic) {
				if (this.fromWaypointIndex >= this.globalWaypoints.Length - 1) {
					this.fromWaypointIndex = 0;
					System.Array.Reverse(this.globalWaypoints);
				}
			}

			this.nextMoveTime = Time.time + this.waitTime;
		}

		return newPos - transform.position;
	}

	private void MovePassengers(bool beforeMovePlatform) {
		foreach (PassengerMovement passenger in this.passengerMovement) {
			if (!this.passengerDictionary.ContainsKey(passenger.transform)) {
				this.passengerDictionary.Add(passenger.transform, passenger.transform.GetComponent<Controller2D>());
			}
			if (passenger.moveBeforePlatform == beforeMovePlatform) {
				this.passengerDictionary[passenger.transform].Move(passenger.velocity, passenger.standingOnPlatform);
			}
		}
	}

	private void CalculatePassengerMovement(Vector3 velocity) {
		HashSet<Transform> movedPassenger = new HashSet<Transform>();
		this.passengerMovement = new List<PassengerMovement>();

		float directionX = Mathf.Sign(velocity.x);
		float directionY = Mathf.Sign(velocity.y);

		// Vertically moving platform
		if (velocity.y != 0) {
			float rayLength = Mathf.Abs(velocity.y) + this.skinWidth;

			for (int i = 0; i < this.verticalRayCount; i++) {
				Vector2 rayOrigin = (directionY == -1) ? this.raycastOrigins.bottomLeft : this.raycastOrigins.topLeft;
				rayOrigin += Vector2.right * (this.verticalRaySpacing * i);
				RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.up * directionY, rayLength, this.passengerMask);

				if (hit && hit.distance != 0) {
					if (!movedPassenger.Contains(hit.transform)) {
						movedPassenger.Add(hit.transform);
						float pushX = (directionY == 1) ? velocity.x : 0;
						float pushY = velocity.y - (hit.distance - this.skinWidth) * directionY;

						this.passengerMovement.Add(new PassengerMovement(hit.transform, new Vector3(pushX, pushY), directionY == 1, true));
					}
				}
			}
		}

		// Horizontally moving platform
		if (velocity.x != 0) {
			float rayLength = Mathf.Abs(velocity.x) + this.skinWidth;

			for (int i = 0; i < this.horizontalRayCount; i++) {
				Vector2 rayOrigin = (directionX == -1) ? this.raycastOrigins.bottomLeft : this.raycastOrigins.bottomRight;
				rayOrigin += Vector2.up * (this.horizontalRaySpacing * i);
				RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.right * directionX, rayLength, this.passengerMask);

				if (hit && hit.distance != 0) {
					if (!movedPassenger.Contains(hit.transform)) {
						movedPassenger.Add(hit.transform);
						float pushX = velocity.x - (hit.distance - this.skinWidth) * directionX;
						float pushY = -this.skinWidth;

						this.passengerMovement.Add(new PassengerMovement(hit.transform, new Vector3(pushX, pushY), false, true));
					}
				}
			}
		}

		// Passenger on top of horizontally or downward moving platform
		if (directionY == -1 || velocity.y == 0 && velocity.x != 0) {
			float rayLength = this.skinWidth * 2;

			for (int i = 0; i < this.verticalRayCount; i++) {
				Vector2 rayOrigin = this.raycastOrigins.topLeft + Vector2.right * (this.verticalRaySpacing * i);
				RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.up, rayLength, this.passengerMask);

				if (hit && hit.distance != 0) {
					if (!movedPassenger.Contains(hit.transform)) {
						movedPassenger.Add(hit.transform);
						float pushX = velocity.x;
						float pushY = velocity.y;

						this.passengerMovement.Add(new PassengerMovement(hit.transform, new Vector3(pushX, pushY), true, false));
					}
				}
			}
		}
	}

	private void OnDrawGizmos() {
		if (this.localWaypoints != null) {
			Gizmos.color = Color.red;
			float gizmoSize = .3f;

			for (int i = 0; i < this.localWaypoints.Length; i++) {
				Vector3 globalWaypointPosition = Application.isPlaying ? this.globalWaypoints[i] : (this.localWaypoints[i] + transform.position);
				Gizmos.DrawLine(globalWaypointPosition - Vector3.up * gizmoSize, globalWaypointPosition + Vector3.up * gizmoSize);
				Gizmos.DrawLine(globalWaypointPosition - Vector3.left * gizmoSize, globalWaypointPosition + Vector3.left * gizmoSize);
			}
		}
	}
}
