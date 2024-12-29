using Sandbox;
using Sandbox.Citizen;
using Sandbox.Internal;
using System;
using System.Diagnostics;

public sealed class PlayerController : Component
{

	[Property]
	CitizenAnimationHelper PlayerAnimationHelper { get; set; }

	[Property]
	public float Acceleration { get; set; } = 400;

	[Property]
	float JumpSpeed { get; set; } = 480;

	[Property]
	float Gravity { get; set; } = 850;

	[Property]
	float MaxFallSpeed { get; set; } = 1600;

	[Property]
	float MinFallSpeed { get; set; } = 800;

	[Property]
	public TetrisController Controller { get; set; }

	float FallSpeed => MathX.Lerp( MaxFallSpeed, MinFallSpeed, Input.AnalogMove.x.Clamp( -1, 0 ) + 1 );

	public BBox BoundingBox => new BBox( new Vector3( -16, -16, 0f ), new Vector3( 16, 16, 64 ) );

	bool FacingLeft = true;
	[Sync]
	float ModelYaw { get; set; } = 180;

	[Sync]
	public Vector3 Velocity { get; set; }

	[Sync]
	public float WishVelocity { get; set; }

	bool IsGround => Scene.Trace.Box( BoundingBox, Transform.Position, Transform.Position + new Vector3( 0, 0, -1 ) ).Run().Hit;

	bool LastGround;

	public static PlayerController Local;

	protected override void OnStart()
	{
		if ( !IsProxy )
		{
			Local = this;
		}
		PlayerAnimationHelper.HoldType = CitizenAnimationHelper.HoldTypes.Punch;
		DressUp();
	}

	protected override void DrawGizmos()
	{
		Gizmo.Draw.LineBBox( BoundingBox );
	}

	protected override void OnFixedUpdate()
	{
		if ( !IsProxy )
		{
			UpdateMove();
			UpdateAction();
			CheckOutOfMap();
		}
	}

	protected override void OnUpdate()
	{
		UpdateAnimation();
	}

	void UpdateMove()
	{
		if ( !Input.AnalogMove.y.AlmostEqual( 0 ) )
		{
			FacingLeft = Input.AnalogMove.y > 0;
		}

		WishVelocity = Input.AnalogMove.y * 320;
		Velocity = Velocity.WithY( Velocity.y.Approach( WishVelocity, Time.Delta * Acceleration ) );

		Velocity = Velocity.WithZ( Velocity.z.Approach( -FallSpeed, Time.Delta * Gravity ) );
		Velocity += Scene.PhysicsWorld.Gravity * Time.Delta;
		if ( IsGround )
		{
			if ( Input.Pressed( "jump" ) )
			{
				Velocity += Vector3.Up * JumpSpeed;
				TriggerJump();
			}
		}
		else
		{
			if ( Input.Pressed( "jump" ) && !Input.AnalogMove.y.AlmostEqual( 0 ) )
			{
				Vector3 direction;
				if ( FacingLeft )
				{
					direction = Vector3.Left;
				}
				else
				{
					direction = Vector3.Right;
				}
				Vector3 tracePosition = Transform.Position + direction * 2;
				SceneTraceResult traceResult = Scene.Trace.Box( BoundingBox, tracePosition, tracePosition ).Run();
				if ( traceResult.Hit )
				{
					Velocity = Vector3.Up * JumpSpeed - direction * 200;
					TriggerJump();
				}
			}
		}

		if ( Scene.Trace.Box( BoundingBox, Transform.Position, Transform.Position ).Run().StartedSolid )
		{
			Unstuck();
		}

		float timeLeft = Time.Delta;
		for ( int i = 0; i < 2; i++ )
		{
			if ( Velocity.Length.AlmostEqual( 0f ) )
			{
				break;
			}
			var traceResult = Scene.Trace.Box( BoundingBox, Transform.Position, Transform.Position + Velocity * timeLeft ).Run();
			if ( !traceResult.Hit )
			{
				Transform.Position = traceResult.EndPosition;
				timeLeft = 0f;
				break;
			}
			else
			{
				Transform.Position = traceResult.EndPosition;
				Velocity = Vector3.VectorPlaneProject( Velocity, traceResult.Normal );
				timeLeft *= 1 - traceResult.Fraction;
			}
		}
		if ( timeLeft > 0 )
		{
			Velocity = Vector3.Zero;
		}

	}

	void UpdateAction()
	{
		Vector3 traceEnd;
		if ( Input.Pressed( "Kick" ) )
		{
			if ( Input.Down( "Backward" ) )
			{
				traceEnd = Transform.Position + Vector3.Down * 16;
				var traceResult = Scene.Trace.Box( BoundingBox, Transform.Position, traceEnd ).Run();
				if ( traceResult.Hit )
				{
					Shape hitShape = traceResult.GameObject.Components.GetInParent<Shape>();
					if ( hitShape is not null && Controller is not null )
					{
						if ( hitShape.isCurrent && hitShape.MoveShape( Vector2Int.Down ) )
						{
							Transform.Position += Vector3.Down * 32;
						}
						else
						{
							hitShape.TimeSinceFall += Controller.FallTime;
						}
						//Log.Info( "Move Down" );
					}
				}
			}
			else
			{
				TriggerPunch();
				traceEnd = Transform.Position + (FacingLeft ? Vector3.Left * 16 : Vector3.Right * 16);
				var traceResult = Scene.Trace.Box( BoundingBox, Transform.Position, traceEnd ).Run();
				if ( traceResult.Hit )
				{
					Shape hitShape = traceResult.GameObject.Components.GetInParent<Shape>();
					if ( hitShape is not null )
					{
						if ( hitShape.isCurrent && hitShape.MoveShape( FacingLeft ? Vector2Int.Right : Vector2Int.Left ) )
						{
							//Transform.Position += (FacingLeft ? Vector2Int.Right : Vector2Int.Left) * 32;
							//Log.Info( $"Move {(FacingLeft ? "Left" : "Right")}" );
						}
					}
				}
			}

		}

		if ( Input.Pressed( "Rotate" ) )
		{
			TriggerPunch();
			traceEnd = Transform.Position + (FacingLeft ? Vector3.Left * 16 : Vector3.Right * 16);
			var traceResult = Scene.Trace.Box( BoundingBox, Transform.Position, traceEnd ).Run();
			if ( traceResult.Hit )
			{
				Shape hitShape = traceResult.GameObject.Components.GetInParent<Shape>();
				if ( hitShape is not null )
				{
					if ( hitShape.isCurrent && hitShape.RotateShape( !FacingLeft ) )
					{
						//Log.Info( $"Rotate {(FacingLeft ? "Left" : "Right")}" );
					}
				}
			}
		}
	}

	void UpdateAnimation()
	{
		if ( !IsProxy )
		{
			ModelYaw = ModelYaw.Approach( FacingLeft ? 90 : 270, 360 * Time.Delta );
		}
		PlayerAnimationHelper.Transform.Rotation = Rotation.FromYaw( ModelYaw );
		PlayerAnimationHelper.WithVelocity( Velocity );
		PlayerAnimationHelper.WithWishVelocity( Vector3.Left * WishVelocity );
		PlayerAnimationHelper.IsGrounded = IsGround;
	}

	void Unstuck()
	{
		Vector3 targetPosition;
		BBox halfBoundingBox = new BBox( BoundingBox.Mins, BoundingBox.Maxs.WithZ( BoundingBox.Maxs.z / 2 ) );
		var result = Scene.Trace.Box( halfBoundingBox, Transform.Position, Transform.Position + Vector3.Up * 32 ).Run();
		if ( !result.StartedSolid )
		{
			if ( !result.Hit )
			{
				return;
			}
			targetPosition = result.EndPosition + Vector3.Down * 33;
			if ( !Scene.Trace.Box( BoundingBox, targetPosition, targetPosition ).Run().StartedSolid )
			{
				Transform.Position = targetPosition;
				return;
			}
		}
		for ( int i = 1; i < 3; i++ )
		{
			if ( FacingLeft )
			{
				targetPosition = Transform.Position + Vector3.Right * 32 * i;
				if ( !Scene.Trace.Box( BoundingBox, targetPosition, targetPosition ).Run().StartedSolid )
				{
					Transform.Position = targetPosition;
					return;
				}
				targetPosition = Transform.Position + Vector3.Up * 32 * i;
				if ( !Scene.Trace.Box( BoundingBox, targetPosition, targetPosition ).Run().StartedSolid )
				{
					Transform.Position = targetPosition;
					return;
				}
				targetPosition = Transform.Position + Vector3.Left * 32 * i;
				if ( !Scene.Trace.Box( BoundingBox, targetPosition, targetPosition ).Run().StartedSolid )
				{
					Transform.Position = targetPosition;
					return;
				}
			}
			else
			{
				targetPosition = Transform.Position + Vector3.Left * 32 * i;
				if ( !Scene.Trace.Box( BoundingBox, targetPosition, targetPosition ).Run().StartedSolid )
				{
					Transform.Position = targetPosition;
					return;
				}
				targetPosition = Transform.Position + Vector3.Up * 32 * i;
				if ( !Scene.Trace.Box( BoundingBox, targetPosition, targetPosition ).Run().StartedSolid )
				{
					Transform.Position = targetPosition;
					return;
				}
				targetPosition = Transform.Position + Vector3.Right * 32 * i;
				if ( !Scene.Trace.Box( BoundingBox, targetPosition, targetPosition ).Run().StartedSolid )
				{
					Transform.Position = targetPosition;
					return;
				}
			}
		}
		if ( Controller is not null )
		{
			Transform.Position = Transform.Position.WithY( Controller.Transform.Position.y + (FacingLeft ? -192 : 192) );
		}
	}

	void CheckOutOfMap()
	{
		if ( Transform.Position.z < -400 )
		{
			if ( Controller is null )
			{
				Transform.Position = Vector3.Zero;
			}
			else
			{
				Transform.Position = new Vector3( 0, Controller.Transform.Position.y + (FacingLeft ? -192 : 192), 0 );
			}
		}
	}

	[Broadcast( NetPermission.OwnerOnly )]
	void TriggerJump()
	{
		PlayerAnimationHelper.TriggerJump();
	}

	[Broadcast( NetPermission.OwnerOnly )]
	void TriggerPunch()
	{
		PlayerAnimationHelper.Target.Set( "b_attack", true );
	}

	void DressUp()
	{
		Connection playerConnection;
		if ( Network.Active )
		{
			playerConnection = Network.OwnerConnection;
		}
		else
		{
			playerConnection = Connection.Local;
		}
		var clothing = new ClothingContainer();
		clothing.Deserialize( playerConnection.GetUserData( "avatar" ) );
		clothing.Apply( Components.GetInChildren<SkinnedModelRenderer>() );
	}
}
