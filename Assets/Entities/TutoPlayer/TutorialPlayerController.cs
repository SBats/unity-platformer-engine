using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent (typeof (Controller2D))]
public class TutorialPlayerController : MonoBehaviour {

  public float jumpHeight = 3f;
  public float timeToJumpApex = .4f;
  public float accelerationTimeAirborne = .2f;
  public float accelerationTimeGrounded = .1f;
  public float moveSpeed = 6f;

  private float gravity;
  private float jumpVelocity;
  private Vector3 velocity;
  private float velocityXSmoothing;
  private Controller2D controller;

  private void Start() {
    controller = GetComponent<Controller2D>();

    this.gravity = -(2 * this.jumpHeight) / Mathf.Pow(this.timeToJumpApex, 2);
    this.jumpVelocity = Mathf.Abs(this.gravity) * this.timeToJumpApex;
  }

  private void Update() {

    if (this.controller.collisions.above || this.controller.collisions.bellow) {
      this.velocity.y = 0;
    }

    Vector2 input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));

    if (Input.GetKeyDown(KeyCode.Space) && this.controller.collisions.bellow) {
      this.velocity.y = this.jumpVelocity;
    }

    float targetVelocityX = input.x * this.moveSpeed;
    this.velocity.x = Mathf.SmoothDamp(velocity.x, targetVelocityX, ref this.velocityXSmoothing, this.controller.collisions.bellow ? this.accelerationTimeGrounded : this.accelerationTimeAirborne);
    this.velocity.y += this.gravity * Time.deltaTime;
    this.controller.Move(this.velocity * Time.deltaTime);
  }
}
