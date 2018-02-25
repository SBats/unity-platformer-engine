using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour {

	public float xDistanceToPeak = 3f;
	public float XSpeed = 10f;
	public float jumpHeight = 5f;
	public float gravityFallFactor = 1.2f;
	public float jumpCancelFactor = 2f;
	public float groundSpeedFactor = 1f;
	public float airSpeedFactor = 1.2f;
	public float skinWidth = .4f;
	public int rayLength = 10;
	public float maxSlopeAngle = 90;

	private float _jumpVelocity;
	private float _gravity;
	private Rigidbody2D _rb2d;
	private BoxCollider2D _collider;
	private int _colLayerMask;
	private bool _aboveSlope = false;
	private bool _onSlope = false;
	private bool _grounded = false;
	private bool _jumping = false;
	private bool _running = false;
	private Vector2 _currentSpeed = new Vector2(0, 0);
	private ContactFilter2D _filter2D = new ContactFilter2D();

	// Use this for initialization
	void Awake () {
		this._rb2d = GetComponent<Rigidbody2D>();
		this._collider = GetComponent<BoxCollider2D>();
		this._colLayerMask = 1 << LayerMask.NameToLayer("Ground");
		this._filter2D.SetLayerMask(this._colLayerMask);
	}

	// Update is called once per frame
	void FixedUpdate () {
		ApplyMovement();
	}

	private void ApplyMovement() {
		float _minYlimit = ComputeLimitBottom();
		float _maxYlimit = ComputeLimitTop();
		float _minXlimit = ComputeLimitLeft();
		float _maxXlimit = ComputeLimitRight();
		float _minYPosition = _minYlimit + this._collider.bounds.extents.y + this.skinWidth;
		float _maxYPosition = _maxYlimit - this._collider.bounds.extents.y - this.skinWidth;
		float _minXPosition = _minXlimit + this._collider.bounds.extents.x + this.skinWidth;
		float _maxXPosition = _maxXlimit - this._collider.bounds.extents.x - this.skinWidth;

		// Debug.DrawLine(new Vector3(_minXlimit, _minYlimit, 0), new Vector3(_maxXlimit, _minYlimit, 0), Color.green);
		// Debug.DrawLine(new Vector3(_minXlimit, _maxYlimit, 0), new Vector3(_maxXlimit, _maxYlimit, 0), Color.green);
		// Debug.DrawLine(new Vector3(_minXlimit, _minYlimit, 0), new Vector3(_minXlimit, _maxYlimit, 0), Color.green);
		// Debug.DrawLine(new Vector3(_maxXlimit, _minYlimit, 0), new Vector3(_maxXlimit, _maxYlimit, 0), Color.green);

		// this._grounded = this._collider.bounds.min.y <= (this._aboveSlope ? _minYlimit + 5 : _minYlimit + 1);
		this._grounded = this._collider.bounds.min.y <= _minYlimit;
		bool _topBlocked = this._collider.bounds.max.y >= _maxYlimit;
		bool _leftBlocked = this._collider.bounds.min.x <= _minXlimit;
		bool _rightBlocked = this._collider.bounds.max.x >= _maxXlimit;

		bool _jumpRequested = (this._grounded && Input.GetButtonDown("Jump"));

		float _horizontalInput = Input.GetAxisRaw("Horizontal");
		if (_horizontalInput != 0) {
			this._running = true;
		}
		float xSpeedFactor = this._grounded ? this.groundSpeedFactor : this.airSpeedFactor;
		this._currentSpeed.x = _horizontalInput * this.XSpeed * xSpeedFactor;

		this._jumpVelocity = (2 * this.jumpHeight * this.XSpeed) / this.xDistanceToPeak;
		this._gravity = GetGravity();

		if (this._grounded) {
			if (_jumpRequested) {
				this._currentSpeed.y = this._jumpVelocity;
				this._jumping = true;
				this._grounded = false;
			} else {
				this._currentSpeed.y = 0;
				// this._onSlope = this._aboveSlope;
			}

		// If in the air, apply gravity
		} else {
			this._currentSpeed.y += this._gravity * Time.deltaTime;
			// this._onSlope = false;
		}

		// If left/right blocked, turn off horizontal speed
		if ((_leftBlocked && this._currentSpeed.x < 0) || (_rightBlocked && this._currentSpeed.x > 0)) {
			this._currentSpeed.x = 0;
		}

		// Clear horizontal movement if under treshold
		// if (Mathf.Abs(this._currentSpeed.x) < this.minSpeedThreshold) {
		// 	this._currentSpeed.x = 0;
		// }

		if (this._jumping) {
			// If top blocked while jumping, turn off vertical speed
			if (_topBlocked) {
				this._currentSpeed.y = 0;
			}

			// Exit jumping state when apex reached
			if (this._currentSpeed.y <= 0) {
				this._jumping = false;
			}
		}

		// Snap controller to ground if on slope
		// if (this._onSlope && !this._jumping) {
		// 	transform.position = new Vector3(
		// 		transform.position.x,
		// 		_minYPosition,
		// 		transform.position.z
		// 	);
		// // Else, apply vertical speed
		// } else {
		// 	transform.position = new Vector3(
		// 		transform.position.x,
		// 		Mathf.Clamp(transform.position.y + this._currentSpeed.y * Time.deltaTime, _minYPosition, _maxYPosition),
		// 		transform.position.z
		// 	);
		// }

		transform.position = new Vector3(
			Mathf.Clamp(transform.position.x + this._currentSpeed.x * Time.deltaTime, _minXPosition, _maxXPosition),
			Mathf.Clamp(transform.position.y + this._currentSpeed.y * Time.deltaTime, _minYPosition, _maxYPosition),
			transform.position.z
		);

		this._running = false;
	}

	private float GetGravity ()	{
		float factor = 1;
		if (this._currentSpeed.y <= 0) {
			factor *= this.gravityFallFactor;
		} else if (!Input.GetButton("Jump")) {
			factor *= this.jumpCancelFactor;
		}
		return (-2 * this.jumpHeight * Mathf.Pow(this.XSpeed, 2)) / Mathf.Pow(this.xDistanceToPeak, 2) * factor;
	}

	private float ComputeLimitBottom() {
		Vector2 _LeftRayOrigin = new Vector2(this._collider.bounds.min.x + skinWidth, this._collider.bounds.min.y);
		Vector2 _RightRayOrigin = new Vector2(this._collider.bounds.max.x - skinWidth, this._collider.bounds.min.y);
		// Vector2 _middleRayOrigin = new Vector2(this._collider.bounds.center.x, this._collider.bounds.min.y);

		Vector2 _LeftLimit = _LeftRayOrigin + Vector2.down * this.rayLength;
		Vector2 _RightLimit = _RightRayOrigin + Vector2.down * this.rayLength;
		// Vector2 _middleLimit = _middleRayOrigin + Vector2.down * this.rayLength;

		// bool slopeLeft = false;
		// bool slopeRight = false;

		RaycastHit2D[] _leftHitResults = new RaycastHit2D[1];
		RaycastHit2D[] _rightHitResults = new RaycastHit2D[1];

		Physics2D.Raycast(_LeftRayOrigin, Vector2.down, this._filter2D, _leftHitResults, this.rayLength);
		Physics2D.Raycast(_RightRayOrigin, Vector2.down, this._filter2D, _rightHitResults, this.rayLength);
		// Debug.DrawRay(_LeftRayOrigin, Vector2.down * this.rayLength, Color.red, 1);
		// Debug.DrawRay(_RightRayOrigin, Vector2.down * this.rayLength, Color.red, 1);

		if (_leftHitResults[0].collider != null) {
			_LeftLimit = _leftHitResults[0].point;
			// slopeLeft = Mathf.Abs(Vector2.Angle(_leftHitResults[0].normal, Vector2.right) - 90) >= 5;
		}

		if (_rightHitResults[0].collider != null) {
			_RightLimit = _rightHitResults[0].point;
			// slopeRight = Mathf.Abs(Vector2.Angle(_rightHitResults[0].normal, Vector2.right) - 90) >= 5;
		}

		// this._aboveSlope = (slopeLeft && slopeRight) ||
		// 								(slopeLeft && !slopeRight && _LeftLimit.y >= _RightLimit.y) ||
		// 								(!slopeLeft && slopeRight && _RightLimit.y >= _LeftLimit.y);

		// if (this._aboveSlope) {
		// 	RaycastHit2D[] _middleHitResults = new RaycastHit2D[1];
		// 	Physics2D.Raycast(_middleRayOrigin, Vector2.down, filter2D, _middleHitResults, this.rayLength);
			// Debug.DrawRay(_middleRayOrigin, Vector2.down, Color.red, 1);

		// 	if (_middleHitResults[0].collider != null) {
		// 		_middleLimit = _middleHitResults[0].point;
		// 	}

		// 	if (slopeLeft && _LeftLimit.y - _middleLimit.y > 5) {
		// 		return _LeftLimit.y;
		// 	} else if (slopeRight && _RightLimit.y - _middleLimit.y > 5) {
		// 		return _RightLimit.y;
		// 	} else {
		// 		return _middleLimit.y;
		// 	}

		// } else {
		// 	return Mathf.Max(_LeftLimit.y, _RightLimit.y);
		// }
		return Mathf.Max(_LeftLimit.y, _RightLimit.y);
	}

	private float ComputeLimitTop() {
		Vector2 _LeftRayOrigin = new Vector2(this._collider.bounds.min.x + skinWidth, this._collider.bounds.max.y);
		Vector2 _RightRayOrigin = new Vector2(this._collider.bounds.max.x - skinWidth, this._collider.bounds.max.y);

		Vector2 _LeftLimit = _LeftRayOrigin + Vector2.up * this.rayLength;
		Vector2 _RightLimit = _RightRayOrigin + Vector2.up * this.rayLength;

		RaycastHit2D[] _leftHitResults = new RaycastHit2D[1];
		RaycastHit2D[] _rightHitResults = new RaycastHit2D[1];

		Physics2D.Raycast(_LeftRayOrigin, Vector2.up, this._filter2D, _leftHitResults, this.rayLength);
		Physics2D.Raycast(_RightRayOrigin, Vector2.up, this._filter2D, _rightHitResults, this.rayLength);
		// Debug.DrawRay(_LeftRayOrigin, Vector2.up * this.rayLength, Color.red, 1);
		// Debug.DrawRay(_RightRayOrigin, Vector2.up * this.rayLength, Color.red, 1);

		if (_leftHitResults[0].collider != null) {
			_LeftLimit = _leftHitResults[0].point;
		}

		if (_rightHitResults[0].collider != null) {
			_RightLimit = _rightHitResults[0].point;
		}

		return Mathf.Min(_LeftLimit.y, _RightLimit.y);
	}

	private float ComputeLimitLeft() {
		Vector2 _topRayOrigin = new Vector2(this._collider.bounds.min.x, this._collider.bounds.max.y - skinWidth);
		Vector2 _middleRayOrigin = new Vector2(this._collider.bounds.min.x, this._collider.bounds.center.y);
		Vector2 _bottomRayOrigin = new Vector2(this._collider.bounds.min.x, this._collider.bounds.min.y + skinWidth);

		Vector2 _topLimit = _topRayOrigin + Vector2.left * this.rayLength;
		Vector2 _middleLimit = _middleRayOrigin + Vector2.left * this.rayLength;
		Vector2 _bottomLimit = _bottomRayOrigin + Vector2.left * this.rayLength;

		RaycastHit2D[] _topHitResults = new RaycastHit2D[1];
		RaycastHit2D[] _middleHitResults = new RaycastHit2D[1];
		RaycastHit2D[] _bottomHitResults = new RaycastHit2D[1];

		Physics2D.Raycast(_topRayOrigin, Vector2.left, this._filter2D, _topHitResults, this.rayLength);
		Physics2D.Raycast(_middleRayOrigin, Vector2.left, this._filter2D, _middleHitResults, this.rayLength);
		Physics2D.Raycast(_bottomRayOrigin, Vector2.left, this._filter2D, _bottomHitResults, this.rayLength);
		// Debug.DrawRay(_topRayOrigin, Vector2.left * this.rayLength, Color.red, 1);
		// Debug.DrawRay(_middleRayOrigin, Vector2.left * this.rayLength, Color.red, 1);
		// Debug.DrawRay(_bottomRayOrigin, Vector2.left * this.rayLength, Color.red, 1);

		if (_topHitResults[0].collider != null && Vector3.Angle(_topHitResults[0].normal, Vector3.left) > this.maxSlopeAngle) {
			_topLimit = _topHitResults[0].point;
		}

		if (_middleHitResults[0].collider != null && Vector3.Angle(_middleHitResults[0].normal, Vector3.left) > this.maxSlopeAngle) {
			_middleLimit = _middleHitResults[0].point;
		}

		if (_bottomHitResults[0].collider != null && Vector3.Angle(_bottomHitResults[0].normal, Vector3.left) > this.maxSlopeAngle) {
			_bottomLimit = _bottomHitResults[0].point;
		}

		return Mathf.Max(_topLimit.x, _middleLimit.x, _bottomLimit.x);
	}

	private float ComputeLimitRight() {
		Vector2 _topRayOrigin = new Vector2(this._collider.bounds.max.x, this._collider.bounds.max.y - skinWidth);
		Vector2 _middleRayOrigin = new Vector2(this._collider.bounds.max.x, this._collider.bounds.center.y);
		Vector2 _bottomRayOrigin = new Vector2(this._collider.bounds.max.x, this._collider.bounds.min.y + skinWidth);

		Vector2 _topLimit = _topRayOrigin + Vector2.right * this.rayLength;
		Vector2 _middleLimit = _middleRayOrigin + Vector2.right * this.rayLength;
		Vector2 _bottomLimit = _bottomRayOrigin + Vector2.right * this.rayLength;

		RaycastHit2D[] _topHitResults = new RaycastHit2D[1];
		RaycastHit2D[] _middleHitResults = new RaycastHit2D[1];
		RaycastHit2D[] _bottomHitResults = new RaycastHit2D[1];

		Physics2D.Raycast(_topRayOrigin, Vector2.right, this._filter2D, _topHitResults, this.rayLength);
		Physics2D.Raycast(_middleRayOrigin, Vector2.right, this._filter2D, _middleHitResults, this.rayLength);
		Physics2D.Raycast(_bottomRayOrigin, Vector2.right, this._filter2D, _bottomHitResults, this.rayLength);
		// Debug.DrawRay(_topRayOrigin, Vector2.right * this.rayLength, Color.red, 1);
		// Debug.DrawRay(_middleRayOrigin, Vector2.right * this.rayLength, Color.red, 1);
		// Debug.DrawRay(_bottomRayOrigin, Vector2.right * this.rayLength, Color.red, 1);

		if (_topHitResults[0].collider != null && Vector3.Angle(_topHitResults[0].normal, Vector3.right) > this.maxSlopeAngle) {
			_topLimit = _topHitResults[0].point;
		}

		if (_middleHitResults[0].collider != null && Vector3.Angle(_middleHitResults[0].normal, Vector3.right) > this.maxSlopeAngle) {
			_middleLimit = _middleHitResults[0].point;
		}

		if (_bottomHitResults[0].collider != null && Vector3.Angle(_bottomHitResults[0].normal, Vector3.right) > this.maxSlopeAngle) {
			_bottomLimit = _bottomHitResults[0].point;
		}

		return Mathf.Min(_topLimit.x, _middleLimit.x, _bottomLimit.x);
	}
}
