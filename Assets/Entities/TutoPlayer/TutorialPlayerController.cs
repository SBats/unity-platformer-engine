using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent (typeof (Controller2D))]
public class TutorialPlayerController : MonoBehaviour {

  public float maxJumpHeight = 3f;
  public float minJumpHeight = 1f;
  public float timeToJumpApex = .4f;
  public int extrajumps = 0;
  public float accelerationTimeAirborne = .2f;
  public float accelerationTimeGrounded = .1f;
  public float moveSpeed = 6f;
  public float wallSlideSpeedMax = 3f;
  public Vector2 wallJumpClimb;
  public Vector2 wallJumpOff;
  public Vector2 wallLeap;
  public float wallStickTime = .25f;

  private float gravity;
  private float maxJumpVelocity;
  private float minJumpVelocity;
  private Vector3 velocity;
  private float velocityXSmoothing;
  private float timeToWallUnstick;
  private Controller2D controller;
  private Vector2 directionalInput;
  private bool wallSliding;
  private int wallDirX;
  private int currentJump;

  private void Start() {
    controller = GetComponent<Controller2D>();

    this.gravity = -(2 * this.maxJumpHeight) / Mathf.Pow(this.timeToJumpApex, 2);
    this.maxJumpVelocity = Mathf.Abs(this.gravity) * this.timeToJumpApex;
    this.minJumpVelocity = Mathf.Sqrt(2 * Mathf.Abs(this.gravity) * this.minJumpHeight);
  }

  private void Update() {
    CalculateVelocity();
    HandleWallSliding();

    this.controller.Move(this.velocity * Time.deltaTime, this.directionalInput);

    if (this.controller.collisions.above || this.controller.collisions.bellow) {
      if (this.controller.collisions.slidingDownMaxSlope) {
        this.velocity.y += this.controller.collisions.slopeNormal.y * -gravity * Time.deltaTime;
      } else {
        this.velocity.y = 0;
      }
    }

    if (this.controller.collisions.bellow || this.wallSliding && this.currentJump == this.extrajumps) {
      this.currentJump = 0;
    }
  }

  public void SetDirectionalInput(Vector2 input) {
    this.directionalInput = input;
  }

  public void OnJumpInputDown() {
    if (this.wallSliding) {
      if (this.wallDirX == this.directionalInput.x) {
        this.velocity.x = -this.wallDirX * this.wallJumpClimb.x;
        this.velocity.y = this.wallJumpClimb.y;
      } else if (this.directionalInput.x == 0) {
        this.velocity.x = -this.wallDirX * this.wallJumpOff.x;
        this.velocity.y = this.wallJumpOff.y;
      } else {
        this.velocity.x = -this.wallDirX * this.wallLeap.x;
        this.velocity.y = this.wallLeap.y;
      }
    }
    if (this.controller.collisions.bellow) {
      this.currentJump = 0;
      if (this.controller.collisions.slidingDownMaxSlope) {
        if (this.directionalInput.x != -Mathf.Sign(this.controller.collisions.slopeNormal.x)) {
          velocity.y = this.maxJumpVelocity * this.controller.collisions.slopeNormal.y;
          velocity.x = this.maxJumpVelocity * this.controller.collisions.slopeNormal.x;
        }
      } else {
        this.velocity.y = this.maxJumpVelocity;
      }
    } else {
      if (!this.wallSliding && this.currentJump < this.extrajumps) {
        this.currentJump++;
        this.velocity.y = this.minJumpVelocity;
      }
    }
  }

  public void OnJumpInputUp() {
    if (this.velocity.y > this.minJumpVelocity) {
     this.velocity.y = this.minJumpVelocity;
    }
  }

  private void CalculateVelocity() {
    float targetVelocityX = this.directionalInput.x * this.moveSpeed;
    this.velocity.x = Mathf.SmoothDamp(this.velocity.x, targetVelocityX, ref this.velocityXSmoothing, this.controller.collisions.bellow ? this.accelerationTimeGrounded : this.accelerationTimeAirborne);
    this.velocity.y += this.gravity * Time.deltaTime;
  }

  private void HandleWallSliding() {
    this.wallDirX = this.controller.collisions.left ? -1 : 1;
    this.wallSliding = false;
    if ((this.controller.collisions.left || this.controller.collisions.right) && !this.controller.collisions.bellow && this.velocity.y <0) {
      this.wallSliding = true;

      if (this.velocity.y < wallSlideSpeedMax) {
        this.velocity.y = -wallSlideSpeedMax;
      }

      if (this.timeToWallUnstick > 0) {
        this.velocityXSmoothing = 0;
        this.velocity.x = 0;

        if (this.directionalInput.x != this.wallDirX && this.directionalInput.x != 0) {
          this.timeToWallUnstick -= Time.deltaTime;
        } else {
          this.timeToWallUnstick = this.wallStickTime;
        }
      } else {
        this.timeToWallUnstick = this.wallStickTime;
      }
    }
  }
}
