using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent (typeof (BoxCollider2D))]
public class RaycastController : MonoBehaviour {

	public LayerMask collisionMask;
	public LayerMask triggerMask;

	public float distBetweenRays = .25f;
	public float skinWidth = .015f;

	[HideInInspector]
	public LayerMask hitMask;
	[HideInInspector]
	public int horizontalRayCount;
	[HideInInspector]
	public int verticalRayCount;
	[HideInInspector]
	public float horizontalRaySpacing;
	[HideInInspector]
	public float verticalRaySpacing;

	[HideInInspector]
	public BoxCollider2D collider;
	public RaycastOrigins raycastOrigins;

	public struct RaycastOrigins {
		public Vector2 topLeft, topRight;
		public Vector2 bottomLeft, bottomRight;
	}

	//
	// Lifecycles
	//

	protected virtual void Awake() {
		this.collider = GetComponent<BoxCollider2D>();
		CalculateRaySpacing();
		this.hitMask = this.collisionMask | this.triggerMask;
	}

	//
	// Public
	//

	public void UpdateRaycastOrigins() {
		Bounds _bounds = this.collider.bounds;
		_bounds.Expand(this.skinWidth * -2);

		this.raycastOrigins.topLeft = new Vector2(_bounds.min.x, _bounds.max.y);
		this.raycastOrigins.topRight = new Vector2(_bounds.max.x, _bounds.max.y);
		this.raycastOrigins.bottomLeft = new Vector2(_bounds.min.x, _bounds.min.y);
		this.raycastOrigins.bottomRight = new Vector2(_bounds.max.x, _bounds.min.y);
	}

	public void CalculateRaySpacing() {
		Bounds _bounds = this.collider.bounds;
		_bounds.Expand(this.skinWidth * -2);

		float boundsWidth = _bounds.size.x;
		float boundsHeight = _bounds.size.y;

		this.horizontalRayCount = Mathf.RoundToInt(boundsHeight / this.distBetweenRays);
		this.verticalRayCount = Mathf.RoundToInt(boundsWidth / this.distBetweenRays);

		this.horizontalRaySpacing = _bounds.size.y / (this.horizontalRayCount - 1);
		this.verticalRaySpacing = _bounds.size.x / (this.verticalRayCount - 1);
	}
}
