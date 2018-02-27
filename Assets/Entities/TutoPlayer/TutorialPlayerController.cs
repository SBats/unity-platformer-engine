using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent (typeof (Controller2D))]
public class TutorialPlayerController : MonoBehaviour {

  public float maxJumpHeight = 3f;
  public float minJumpHeight = 1f;
  public float timeToJumpApex = .4f;
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

  private void Start() {
    controller = GetComponent<Controller2D>();

    this.gravity = -(2 * this.maxJumpHeight) / Mathf.Pow(this.timeToJumpApex, 2);
    this.maxJumpVelocity = Mathf.Abs(this.gravity) * this.timeToJumpApex;
    this.minJumpVelocity = Mathf.Sqrt(2 * Mathf.Abs(this.gravity) * this.minJumpHeight);
  }

  private void Update() {
    Vector2 input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
    int wallDirX = this.controller.collisions.left ? -1 : 1;

    float targetVelocityX = input.x * this.moveSpeed;
    this.velocity.x = Mathf.SmoothDamp(velocity.x, targetVelocityX, ref this.velocityXSmoothing, this.controller.collisions.bellow ? this.accelerationTimeGrounded : this.accelerationTimeAirborne);

    bool wallSliding = false;
    if ((this.controller.collisions.left || this.controller.collisions.right) && !this.controller.collisions.bellow && velocity.y <0) {
      wallSliding = true;

      if (velocity.y < wallSlideSpeedMax) {
        velocity.y = -wallSlideSpeedMax;
      }

      if (this.timeToWallUnstick > 0) {
        this.velocityXSmoothing = 0;
        velocity.x = 0;

        if (input.x != wallDirX && input.x != 0) {
          this.timeToWallUnstick -= Time.deltaTime;
        } else {
          this.timeToWallUnstick = this.wallStickTime;
        }
      } else {
        this.timeToWallUnstick = this.wallStickTime;
      }
    }

    if (Input.GetKeyDown(KeyCode.Space)) {
      if (wallSliding) {
        if (wallDirX == input.x) {
          velocity.x = -wallDirX * this.wallJumpClimb.x;
          velocity.y = this.wallJumpClimb.y;
        } else if (input.x == 0) {
          velocity.x = -wallDirX * this.wallJumpOff.x;
          velocity.y = this.wallJumpOff.y;
        } else {
          velocity.x = -wallDirX * this.wallLeap.x;
          velocity.y = this.wallLeap.y;
        }
      }
      if (this.controller.collisions.bellow) {
        this.velocity.y = this.maxJumpVelocity;
      }
    }
    if (Input.GetKeyUp(KeyCode.Space)) {
      if (velocity.y > this.minJumpVelocity) {
        velocity.y = this.minJumpVelocity;
      }
    }

    this.velocity.y += this.gravity * Time.deltaTime;
    this.controller.Move(this.velocity * Time.deltaTime, input);

    if (this.controller.collisions.above || this.controller.collisions.bellow) {
      this.velocity.y = 0;
    }
  }
}
