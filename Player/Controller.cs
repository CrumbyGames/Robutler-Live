using Godot;
using System;
using System.Collections.Generic;
namespace Player
{

	public enum State
	{
		Idle = 1,
		Move = 2,
		Brake = 4,
		Land = 8,
		Jump = 16,
		Fall = 32,
		Grapple = 64,
		Destroyed = 128,
		Static = Idle | Land,
		Grounded = Idle | Move | Brake | Land,
		Airborne = Jump | Fall
	}

	public class @Controller : KinematicBody2D
	{
		const float TerminalVelocity = 1800.0f;

		// Grounded
		const float MoveAcceleration = 850f;
		const float MaxSpeed = 300f;
		const float JumpSpeed = 350f;
		const float BrakingSpeed = MaxSpeed - 50f;
		const float GroundedGravity = 1f * Gravity;
		const float FloorMaxAngle = 60f * Mathf.Pi / 180f;

		// Airborne
		const float AirControlMod = 1.0f;
		const float Gravity = 1300f;
		const float BrakingDecelerationMod = 2.5f;

		// Grappling
		const float MinGrappleAcceleration = 40f;
		const float MaxGrappleSpeed = 1400f;
		const float GrappleCoefficient = 0.4f;
		const float GrapplePowerDistanceCutoff = 400f;
	

		// Potential tweaks to fundamentals of grappling physics
		const bool GrappleHasGravity = false;
		const bool GrappleHasAirRes = false;

		public Vector2 Velocity = Vector2.Zero;
		public State ActiveState = State.Idle;
		public Vector2 lastHookedGrapplePosition = Vector2.Zero;

		float hookRelativeDistanceTravelled = -1;
		bool shotHook = false;
		Vector2 DebugSpawnPosition = Vector2.Zero; // Where the player respawns if the debugging respawn is use

		DebugLabel debugLabel;
		ShapeCast2D floorCast;
		RayCast2D grappleRopeCast;
		Timer coyoteTimer;
		Line2D speedTrail;
		Node2D hookSprite;
		Particles2D brakingParticles;
		Particles2D jumpParticles;
		Grapple.PointManager grappleManager;
		Grapple.Point grappledPoint = null;
		ParticlesMaterial brakingParticlesMaterial;

		public Grapple.Point GrappledPoint
		{
			get
			{
				return grappledPoint;
			}

			set
			{
				grappledPoint?.SetSuppressOutline(false);
				grappledPoint = value;
				grappledPoint?.SetSuppressOutline(true);

			}
		}

		Color grappleGearColor { get; } = new Color(0.23f, 0.76f, 0.85f);

		public override void _Ready()
		{
			speedTrail = GetNode<Line2D>("Effects/Trail");
			brakingParticles = GetNode<Particles2D>("Effects/BrakingSparks");
			brakingParticlesMaterial = (ParticlesMaterial)brakingParticles.ProcessMaterial;
			jumpParticles = GetNode<Particles2D>("Effects/JumpDust");
			hookSprite = GetNode<Node2D>("Sprite/GrappleHook");
			hookSprite.Modulate = grappleGearColor;
			grappleRopeCast = GetNode<RayCast2D>("grapple");

			coyoteTimer = GetNode<Timer>("coyoteTimer");
			grappleManager = GetNode<Grapple.PointManager>("/root/PointManager");
			floorCast = GetNode<ShapeCast2D>("floor");

			debugLabel = GetNode<DebugLabel>("DebugLabel"); // Handles neat output for debugging purposes

			// For testing purposes: handles respawn
			if (DebugSpawnPosition == Vector2.Zero)
			{
				DebugSpawnPosition = GlobalPosition;
			}
			else
			{
				speedTrail.Set("trail_points", new List<Vector2>());
				GlobalPosition = DebugSpawnPosition;
				Velocity = Vector2.Zero;

			}
		}

		public override void _Process(float delta)
		{
			// Tell engine to keep redrawing grapple every frame
			Update();
		}


		public override void _Draw()
		{
			// Draws grapple hook and rope as long as it's not fully retracted
			if (hookRelativeDistanceTravelled > -1)
			{
				Vector2 direction = GlobalPosition.DirectionTo(lastHookedGrapplePosition);
				Vector2 normal = new Vector2(-direction.y, direction.x);
				float amplitude = 20f;
				float frequency = 0.2f;
				int points = (int)(GlobalPosition.DistanceTo(lastHookedGrapplePosition) * frequency * hookRelativeDistanceTravelled);

				// Lambda expression of calculation
				Func<int, Vector2> getPoint = (int position) => { return direction * position / frequency + normal * Mathf.Sin(position) * (points - position) / points * amplitude * (1 - hookRelativeDistanceTravelled); };

				for (int i = 0; i < points; i++)
				{
					DrawLine(getPoint(i), getPoint(i + 1), grappleGearColor, 2);
				}

				// Because it attaches itself to the end of the grapple rope, regardless of where it is on the sine curve, the hook is updated within _draw.
				hookSprite.Visible = true;
				hookSprite.Position = getPoint(points);
				hookSprite.Rotation = direction.Angle();
				hookSprite.Scale = hookRelativeDistanceTravelled * new Vector2(0.15f, 0.15f); // Discreetly grows in size as grapple extends which makes it look smoother
			}
			else
			{
				hookSprite.Visible = false;
			}
		}

		// Occurs every physics step
		public override void _PhysicsProcess(float delta)
		{
			Velocity = MoveAndSlide(Velocity, Vector2.Up, stopOnSlope: false, floorMaxAngle: FloorMaxAngle);
			State newState = ActiveState;

			// For debugging purposes, adds a few properties that are nice to keep an eye on, depending on what I'm working on.
			debugLabel.AddTextOnLayer(1, "State:", ActiveState);

			debugLabel.AddTextOnLayer(2, "Speed:", Velocity.Length());

			debugLabel.AddTextOnLayer(3, "Distance from point: ", GrappledPoint?.GetGlobalMousePosition().DistanceTo(GlobalPosition));

			debugLabel.AddTextOnLayer(4, "Relative grapple pos: ", hookRelativeDistanceTravelled);

			// Temporary respawn for testing
			if (Input.IsActionJustPressed("restart"))
			{
				newState = State.Destroyed;
			}

			// Inputs
			int moveDirection = Math.Sign(getHorizontalInput());
			bool jumpPressed = Input.IsActionPressed("jump");
			bool mouseLeftPressed = Input.IsActionPressed("mouse_left");

			// Grounded States

			if (newState.MatchesEnum(State.Land)) // Just landed
			{
				// State paths
				if (true) // temporary implementation for future
				{
					newState = State.Idle;
				}
			}

			if (newState.MatchesEnum(State.Static)) // Either idle or just landed
			{
				decelerate(MoveAcceleration * delta);

				// State paths
				if (moveDirection != 0)
				{
					newState = State.Move;
				}
			}

			if (newState.MatchesEnum(State.Move)) // Moving (on ground)
			{
				accelerate(MoveAcceleration * delta, direction: moveDirection);

				// State paths
				if (moveDirection == 0)
				{
					newState = State.Idle;
				}


			}

			if (newState.MatchesEnum(State.Brake)) // Braking
			{
				decelerate(MoveAcceleration * delta * BrakingDecelerationMod);

				brakingParticlesMaterial.InitialVelocity = Mathf.Abs(Velocity.x);
				Vector2 particleDirection = GetFloorNormal().Rotated(Mathf.Pi / 2.2f * Mathf.Sign(Velocity.x));
				brakingParticlesMaterial.Direction = new Vector3(particleDirection.x, particleDirection.y, 0);

				// State paths
				if (moveDirection == 0)
				{
					newState = State.Idle;
				}
				else if (moveDirection == Mathf.Sign(Velocity.x) || Mathf.Abs(Velocity.x) < 50f)
				{
					newState = State.Move;
				}
			}

			if (newState.MatchesEnum(State.Grounded)) // Generally grounded
			{
				Velocity.y += delta * GroundedGravity;

				// State paths
				if (!IsOnFloor())
				{
					newState = State.Fall;
				}
				else if (jumpPressed)
				{
					newState = State.Jump;
				}
				else if (moveDirection != Mathf.Sign(Velocity.x) && Mathf.Abs(Velocity.x) > BrakingSpeed)
				{
					newState = State.Brake;
				}
			}

			// Airborne States

			if (newState.MatchesEnum(State.Fall))
			{ // Downwards velocity while airborne

				// State paths
				if (IsOnFloor())
				{
					newState = State.Land;
				}
				else if (jumpPressed && coyoteTimer.TimeLeft > 0)
				{
					newState = State.Jump;
				}
			}

			if (newState.MatchesEnum(State.Jump)) // The exact moment of jumping (currently has no behaviour other than transitioning immediately to fall)
			{

				// State paths
				if (!IsOnFloor() && (true || Velocity.y > 0)) // temporary implementation for future
				{
					newState = State.Fall;
				}
			}

			if (newState.MatchesEnum(State.Airborne)) // Any airborne state (i.e. not grappling and not grounded)
			{
				if (Velocity.y < TerminalVelocity)
				{
					Velocity.y += Gravity * delta;
				}

				float moddedAcceleration = MoveAcceleration * AirControlMod;
				int direction = Math.Sign(moveDirection);

				if (direction != 0)
				{
					accelerate(moddedAcceleration * delta, direction: direction);
				}
				else
				{
					decelerate(moddedAcceleration * delta / 2, onBothAxes: true);
				}

			}

			// Grappling

			if (newState.MatchesEnum(State.Grapple)) // 
			{
				if (grappledPoint != null)
				{
					if (GrappledPoint.Position.y > Position.y && GrappleHasGravity)
					{
						Velocity.y += Gravity * delta;
					}

					float moddedAcceleration = MoveAcceleration * AirControlMod;
					int direction = Math.Sign(moveDirection);

					if (direction != 0)
					{
						accelerate(moddedAcceleration * delta, direction: direction);
					}
					else if (GrappleHasAirRes)
					{
						decelerate(moddedAcceleration * delta, onBothAxes: true);
					}

					Vector2 pointDirection = GrappledPoint.GlobalPosition - GlobalPosition;
					float pointDistance = pointDirection.Length();
					pointDirection = pointDirection.Normalized();
					float previousSpeed = Velocity.Length();
					Vector2 acceleration = pointDirection * GrappleCoefficient * pointDistance.Clamp(MinGrappleAcceleration / GrappleCoefficient, GrapplePowerDistanceCutoff);

					Velocity += acceleration;

					if (previousSpeed < MaxGrappleSpeed)
					{
						Velocity = Velocity.LimitLength(MaxGrappleSpeed);
					}
					else
					{
						Velocity = Velocity.LimitLength(previousSpeed);
					}
				}

				// State paths
				if (!mouseLeftPressed || GrappledPoint == null)
				{
					newState = State.Fall;
				}
			}

			// If rope is fully retracted, allow launching grapple
			else if (hookRelativeDistanceTravelled == -1)
			{
				if (grappleManager.SelectedPoint != null && !ActiveState.MatchesEnum(State.Destroyed))
				{
					// Update raycast to check if path to point is clear
					Vector2 grappleVector = grappleManager.SelectedPoint.GlobalPosition - this.GlobalPosition;
					grappleRopeCast.CastTo = grappleVector - grappleVector.Normalized() * 5;

					// If path to point is clear and user has clicked: launch hook
					if (mouseLeftPressed && !grappleRopeCast.IsColliding())
					{
						hookRelativeDistanceTravelled = 0;
						shotHook = true;
						GrappledPoint = grappleManager.SelectedPoint;
						lastHookedGrapplePosition = GrappledPoint.GlobalPosition;
					}
				}
			}
			else
			{
				// If rope is being extended, continue extending the rope until it reaches the point. 
				if (shotHook)
				{
					hookRelativeDistanceTravelled = hookRelativeDistanceTravelled.LinearInterpolate(1.05f, 0.2f);
					if (hookRelativeDistanceTravelled >= 1)
					{
						newState = State.Grapple;
					}
				}

				// If rope is being retracted, continue retracting the rope until it reaches the player. 
				else
				{
					hookRelativeDistanceTravelled = hookRelativeDistanceTravelled.LinearInterpolate(-0.05f, 0.25f);
					if (hookRelativeDistanceTravelled <= 0)
					{
						hookRelativeDistanceTravelled = -1;
					}
				}
			}

			// Update state to whatever was transitioned to this physics step
			enterState(newState);
		}

		// Override normal IsOnFloor() behaviour by using shapecast instead
		public new bool IsOnFloor()
		{
			float floorAngle = GetFloorNormal().AngleFrom(Vector2.Up);
			return floorCast.IsColliding() && Mathf.Abs(floorAngle) < FloorMaxAngle;
		}

		// Handle state transitions
		void enterState(State newState)
		{
			if (newState != ActiveState)
			{
				switch (newState)
				{
					case State.Brake:
						brakingParticles.Emitting = true;
						break;
					case State.Jump:
						jump(JumpSpeed);
						coyoteTimer.Stop();
						break;
					case State.Grapple:
						hookRelativeDistanceTravelled = 1;
						shotHook = false;
						coyoteTimer.Stop();
						break;
					case State.Destroyed:
						_Ready(); // Easy respawn for testing
						enterState(State.Fall);
						return;
				}

				if (!newState.MatchesEnum(State.Brake))
				{
					brakingParticles.Emitting = false;
				}

				if (!newState.MatchesEnum(State.Grapple))
				{
					GrappledPoint = null;
				}


				if (IsOnFloor() && !newState.MatchesEnum(State.Jump))
				{
					coyoteTimer.Start();
					coyoteTimer.Paused = true;
				}
				else
				{
					coyoteTimer.Paused = false;
				}

				ActiveState = newState;
			}
		}

		void accelerate(float acceleration, int direction) // Acceleration must be modified by delta in advance
		{
			// *INCREASE MAX SPEED ON SLOPE*
			// float slope = Vector2.Right.Dot(GetFloorNormal());
			// float moddedSpeed = MaxSpeed;
			// if (direction == 1) {
			// 	moddedSpeed += SlopeSpeedMod*MaxSpeed*slope.Clamp(min:0);
			// } else {
			// 	moddedSpeed += SlopeSpeedMod*MaxSpeed*slope.Clamp(max:0);
			// } 

			if (((Math.Abs(Velocity.x) < MaxSpeed) || direction != Math.Sign(Velocity.x)) && direction != 0)
			{
				Velocity.x += acceleration * direction;
			}
		}

		void decelerate(float deceleration, bool onBothAxes = false) // Deceleration must be modified by delta in advance
		{
			if (!onBothAxes)
			{
				if (Math.Abs(Velocity.x) > deceleration)
				{
					Velocity.x -= deceleration * Math.Sign(Velocity.x);
				}
				else
				{
					Velocity.x = 0;
				}
			}
			else
			{
				if (Velocity.Length() > deceleration)
				{
					Velocity -= Velocity.Normalized() * deceleration;
				}
				else
				{
					Velocity = Vector2.Zero;
				}
			}
		}

		void jump(float speed)
		{
			Vector2 normal;
			normal = GetFloorNormal();
			Velocity += speed * normal / 1f;

			jumpParticles.Rotation = normal.AngleFrom(Vector2.Up);
			jumpParticles.Restart();

			// *Ensures that if velocity is downward, the jump is still full height. However, if there is upward velocity, this gets added to jump.*
			// Velocity.y = (Velocity.y - speed).Clamp(max: -speed);
		}


		float getHorizontalInput()
		{
			return Input.GetActionStrength("move_right") - Input.GetActionStrength("move_left");
		}


	}
}
