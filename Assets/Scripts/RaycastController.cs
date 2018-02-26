using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent (typeof (BoxCollider2D))]
public class RaycastController : MonoBehaviour {

	public LayerMask collisionMask;

	public int horizontalRayCount = 4;
	public int verticalRayCount = 4;
	public float skinWidth = .015f;

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

	public virtual void Start() {
		this.collider = GetComponent<BoxCollider2D>();
		CalculateRaySpacing();
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

		this.horizontalRayCount = Mathf.Clamp(this.horizontalRayCount, 2, int.MaxValue);
		this.verticalRayCount = Mathf.Clamp(this.verticalRayCount, 2, int.MaxValue);

		this.horizontalRaySpacing = _bounds.size.y / (this.horizontalRayCount - 1);
		this.verticalRaySpacing = _bounds.size.x / (this.verticalRayCount - 1);
	}
}
