using Cysharp.Threading.Tasks;
using NaughtyAttributes;
using NinjaSchool.Tilemaps;
using RotaryHeart.Lib.SerializableDictionary;
using System;
using System.Runtime.CompilerServices;
using System.Threading;
using UnityEngine;
using UnityEngine.InputSystem;


namespace NinjaSchool
{
	public sealed class Ninja : MonoBehaviour, ICollider
	{
		#region Thông số chiến đấu
#if DEBUG
		[SerializeField]
		[Label("HP")]
#endif
		private float Δhp;
		public float hp
		{
			get => Δhp;
			set
			{
				Δhp = value >= 0 ? value : 0;
			}
		}


#if DEBUG
		[SerializeField]
		[Label("MP")]
#endif
		private float Δmp;
		public float mp
		{
			get => Δmp;
			set
			{
				Δmp = (value >= 0) ? value : 0;
			}
		}


#if DEBUG
		[SerializeField]
		[Label("EXP")]
#endif
		private float Δexp;
		public float exp
		{
			get => Δexp;
			set
			{
				Δexp = value >= 0 ? value : 0;
			}
		}


#if DEBUG
		[SerializeField]
		[Label("LEVEL")]
#endif
		private float Δlevel;
		public float level
		{
			get => Δlevel;
			set
			{
				Δlevel = value >= 0 ? value : 0;
			}
		}


		[Serializable]
		public struct Skill
		{
			[Tooltip("Khinh công nhảy cao: nhảy lên tối đa 3 ô")]
			public bool jumpHigh;

			[Tooltip("Kinh công nhảy siêu cao: nhảy lên tối đa 4 ô")]
			public bool jumpSuperHigh;

			[Tooltip("Độn thổ")]
			public bool hideUnderGround;

			[Tooltip("Tàng hình")]
			public bool stealth;

			[Tooltip("Biến hình đầu đỏ: tăng sức tấn công")]
			public bool superSaiyan;

			[Tooltip("Chạy trên mặt nước")]
			public bool runOnWater;

			[Tooltip("Thở dưới nước: độ sâu không quá 3 ô")]
			public bool breathUnderWater;

			[Tooltip("Đứng nước: không bao giờ bị chìm")]
			public bool standOnWater;

			[Tooltip("Phi tiêu cấp 1: tăng sức sát thương cho phi tiêu")]
			public bool shurikenLv1;

			[Tooltip("Phi tiêu cấp 2: tăng sức sát thương cho phi tiêu")]
			public bool shurikenLv2;

			[Tooltip("Chạy nhanh")]
			public bool runFast;
		}
#if !DEBUG
		[NonSerialized]
#endif
		public Skill skill;
		#endregion


		private Direction currentDirection;
		private UniTask taskDirection;
		public Direction direction
		{
			get => currentDirection;

			set
			{
				if (value == currentDirection || value == default) return;
				currentDirection = value;
				if (value.x < 0 && isRightFace) isRightFace = false;
				else if (value.x > 0 && !isRightFace) isRightFace = true;
				if (taskDirection.isRunning()) return;

				if (value == Direction.down)
				{
					if (!taskAnimAttack.isRunning()) (taskDirection = Sit_HideUnderground()).Forget();
				}
				else if (value.y > 0) (taskDirection = Jump()).Forget();
				else (taskDirection = Run()).Forget();
			}
		}


		private void ForceSetDirection(Direction value)
		{
			taskDirection = UniTask.CompletedTask;
			currentDirection = default;
			direction = value;
		}


		[Serializable]
		private struct Part
		{
			public GameObject obj;
			public SpriteRenderer head;
		}
		[SerializeField] private Part idlePart;
		[SerializeField] private GameObject eyes;
		public Vector2 size { get; } = new Vector2(1, 1);

		private static readonly Quaternion EULER_Y180 = Quaternion.Euler(0, 180, 0);
		private bool isRightFace
		{
			get
			{
				var rot = transform.rotation;
				return rot == Quaternion.identity || (rot == EULER_Y180 ? false : throw new Exception());
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set => transform.rotation = value ? Quaternion.identity : EULER_Y180;
		}


		private void HideAllPartObjs()
		{
			idlePart.obj.SetActive(false);
			sitPart.obj.SetActive(false);
			runningPart.obj.SetActive(false);
			airPart.obj.SetActive(false);
			attackingPart.obj.SetActive(false);
		}


		#region Sit, Hide underground
		[SerializeField] private Part sitPart;
		[Tooltip("Ngồi bao lâu mới có thể độn thổ ?")]
		[SerializeField] private float delayUndergroundSeconds;
		[Tooltip("MP tiêu hao khi độn thổ")]
		[SerializeField] private float mp_hideUnderground;
		public bool isUnderground { get; private set; }
		private static readonly string[] TILE_IDs_CAN_UNDERGROUND = new string[]
		{
			"-1", "0", "1", "2", "3", "4", "5"
		};
		private CancellationToken token_Registered_HideUnderground;


		private async UniTask Sit_HideUnderground()
		{
			var token = cancelAll.Token;
			HideAllPartObjs();
			sitPart.obj.SetActive(true);
			float t = Time.time + delayUndergroundSeconds;
			isUnderground = false;
			bool canHide = true;

			do
			{
				if (canHide && skill.hideUnderGround && mp > 0 && Time.time > t)
				{
					#region Kiểm tra địa hình đang ngồi có thể độn thổ ?
					var pos = transform.position;
					pos.x -= 0.5f;
					var index = new Vector3Int((int)pos.x, (int)(pos.y - 1), 0);

					for (int stopX = Mathf.CeilToInt(pos.x + 1) - 1; index.x <= stopX; ++index.x)
					{
						var tile = Tilemap.GetGroundTile(index);
						if (!tile) continue;

						string id = tile.name;
						for (int i = 0; i < TILE_IDs_CAN_UNDERGROUND.Length; ++i)
							if (TILE_IDs_CAN_UNDERGROUND[i] == id)
							{
								isUnderground = true;
								goto END_SIT;
							}
					}

					canHide = false;
					#endregion
				}

				currentDirection = default;
				await UniTask.NextFrame(PlayerLoopTiming.LastUpdate, token);
			} while (currentDirection.y < 0);
		END_SIT:
			sitPart.obj.SetActive(false);
			if (!isUnderground)
			{
				if (currentDirection != default) ForceSetDirection(currentDirection);
				else idlePart.obj.SetActive(true);
				return;
			}

			#region Hide underground
			transform.position += Vector3.down;
			eyes.SetActive(true);
			if (token_Registered_HideUnderground != token)
				(token_Registered_HideUnderground = token).RegisterWithoutCaptureExecutionContext(Stop);

			do
			{
				mp -= mp_hideUnderground;
				currentDirection = default;
				await UniTask.NextFrame(PlayerLoopTiming.LastUpdate, token);
			} while (mp > 0 && currentDirection.y <= 0);
			Stop();
			currentDirection.y = Direction.upValue;
			(taskDirection = Jump()).Forget();


			void Stop()
			{
				isUnderground = false;
				transform.position += Vector3.up;
				if (token.IsCancellationRequested || !isStealth) eyes.SetActive(false);
				token_Registered_HideUnderground = default;
			}
			#endregion
		}
		#endregion


		#region Run
		[Serializable]
		private struct RunningPart
		{
			public GameObject obj;
			public Part[] parts;
			public float speed;
			public int animDelay;
		}
		[SerializeField] private RunningPart runningPart;
		private CancellationToken token_Registered_Run;


		private async UniTask Run()
		{
			var token = cancelAll.Token;
			if (token_Registered_Run != token)
				(token_Registered_Run = token).RegisterWithoutCaptureExecutionContext(Stop);
			var dirX = currentDirection.x;
			Vector3 v = new Vector3(dirX > 0 ? runningPart.speed : -runningPart.speed, 0), fallDown = new Vector3(0, -airPart.fallSpeed);
			HideAllPartObjs();
			runningPart.obj.SetActive(true);
			bool cancelAnim = false;
			Anim();


			do
			{
				if (dirX != currentDirection.x)
				{
					dirX = currentDirection.x;
					v.x *= -1;
				}
				currentDirection = default;
				var dest = Tilemap.FindMoveDestination(this, v);
				if (dest != transform.position)
				{
					transform.position = dest;
					if (Tilemap.FindMoveDestination(this, fallDown).y < dest.y)
					{
						Stop();
						(taskDirection = Fall()).Forget();
						return;
					}
				}

				await UniTask.Delay(15, delayTiming: PlayerLoopTiming.LastUpdate, cancellationToken: token);
			} while (currentDirection.x != 0 && currentDirection.y <= 0);
			Stop();
			if (currentDirection != default) ForceSetDirection(currentDirection);
			else idlePart.obj.SetActive(true);


			async void Anim()
			{
				while (true)
				{
					for (int i = 0; i < runningPart.parts.Length; ++i)
					{
						var obj = runningPart.parts[i].obj;
						obj.SetActive(true);
						await UniTask.Delay(runningPart.animDelay);
						if (cancelAnim) return;
						obj.SetActive(false);
					}
				}
			}


			void Stop()
			{
				cancelAnim = true;
				runningPart.obj.SetActive(false);
				for (int i = 0; i < runningPart.parts.Length; ++i) runningPart.parts[i].obj.SetActive(false);
				token_Registered_Run = default;
			}
		}
		#endregion


		#region Air Move: Jump, Fall
		[Serializable]
		private struct AirPart
		{
			public GameObject obj;
			public Part[] parts;
			public Animator spin;
			public float jumpSpeed, fallSpeed;
			[Tooltip("MP tiêu hao khi nhảy cao/siêu cao")]
			public float mp_extraJump;
			public int delayAfterJumping;

			public static readonly int SPIN1 = Animator.StringToHash("SPIN1"), SPIN6 = Animator.StringToHash("SPIN6");
		}
		[SerializeField] private AirPart airPart;
		private CancellationToken token_Registered_AirMove;


		private async UniTask Jump()
		{
			var token = cancelAll.Token;
			if (token_Registered_AirMove != token)
				(token_Registered_AirMove = token).RegisterWithoutCaptureExecutionContext(StopAirMove);
			var v = new Vector3(runningPart.speed * (int)currentDirection.x, airPart.jumpSpeed);
			HideAllPartObjs();
			airPart.obj.SetActive(true);
			var partObj = airPart.parts[1].obj;
			partObj.SetActive(true);
			transform.parent = null;

			// Lên y ~= 1.5
			for (int i = 0; i < 3; ++i) if (!await MoveUp()) goto BLOCKED;

			if (!skill.jumpHigh)
			{
				#region Nhảy thấp (chưa học khinh công)
				await UniTask.Delay(airPart.delayAfterJumping, cancellationToken: token);
				partObj.SetActive(false);
				airPart.obj.SetActive(false);
				(taskDirection = Fall()).Forget();
				return;
				#endregion
			}

			currentDirection = default;
			// Lên y ~= 2
			if (!await MoveUp()) goto BLOCKED;

			UniTask taskSpin;
			if (mp == 0 || currentDirection.y <= 0)
			{
				#region Nhảy bình thường sau khi đã học khinh công
				partObj.SetActive(false);
				taskSpin = Spin(true);

				// Kiểm tra nếu nhấn Left/Right thì di chuyển ngang
				Vector3 p;
				if (currentDirection.x != 0
					&& (p = Tilemap.FindMoveDestination(this, new Vector3(runningPart.speed * (int)currentDirection.x, 0))) != transform.position)
				{
					transform.position = p;
					await UniTask.WhenAll(taskSpin, UniTask.Delay(15, cancellationToken: token));
				}
				else await taskSpin;

				airPart.parts[0].obj.SetActive(false);
				airPart.obj.SetActive(false);
				(taskDirection = Fall()).Forget();
				return;
				#endregion
			}

			mp -= airPart.mp_extraJump;
			partObj.SetActive(false);
			await Spin(false);

			// Lên y ~= 2.5
			if (!await MoveUp()) goto BLOCKED;
			currentDirection = default;
			// Lên y ~= 3
			if (!await MoveUp()) goto BLOCKED;

			if (!skill.jumpSuperHigh || mp == 0 || currentDirection.y <= 0)
			{
				#region Nhảy cao
				partObj.SetActive(false);
				taskSpin = Spin(true);

				// Kiểm tra nếu nhấn Left/Right thì di chuyển ngang
				Vector3 p;
				if (currentDirection.x != 0
					&& (p = Tilemap.FindMoveDestination(this, new Vector3(runningPart.speed * (int)currentDirection.x, 0))) != transform.position)
				{
					transform.position = p;
					await UniTask.WhenAll(taskSpin, UniTask.Delay(15, cancellationToken: token));
				}
				else await taskSpin;

				airPart.parts[0].obj.SetActive(false);
				airPart.obj.SetActive(false);
				(taskDirection = Fall()).Forget();
				return;
				#endregion
			}

			mp -= airPart.mp_extraJump;
			partObj.SetActive(false);
			await Spin(false);

			// Lên y ~= 3.5
			if (!await MoveUp()) goto BLOCKED;
			currentDirection = default;
			// Lên y ~= 4
			if (!await MoveUp()) goto BLOCKED;

			#region Nhảy siêu cao
			partObj.SetActive(false);
			taskSpin = Spin(true);

			// Kiểm tra nếu nhấn Left/Right thì di chuyển ngang
			Vector3 pp;
			if (currentDirection.x != 0
				&& (pp = Tilemap.FindMoveDestination(this, new Vector3(runningPart.speed * (int)currentDirection.x, 0))) != transform.position)
			{
				transform.position = pp;
				await UniTask.WhenAll(taskSpin, UniTask.Delay(15, cancellationToken: token));
			}
			else await taskSpin;

			airPart.parts[0].obj.SetActive(false);
			airPart.obj.SetActive(false);
			(taskDirection = Fall()).Forget();
			return;
		#endregion

		BLOCKED:
			partObj.SetActive(false);
			airPart.obj.SetActive(false);
			(taskDirection = Fall()).Forget();


			Vector3 pos, dest;
			async UniTask<bool> MoveUp()
			{
				float d = 0;
				while ((dest = Tilemap.FindMoveDestination(this, v)).y > (pos = transform.position).y)
				{
					transform.position = dest;
					await UniTask.Delay(15, delayTiming: PlayerLoopTiming.LastUpdate, cancellationToken: token);
					v.x = currentDirection.x != 0 ? runningPart.speed * (int)currentDirection.x : v.x;
					if ((d += dest.y - pos.y) >= 0.5f) return true;
				}
				transform.position = dest;
				return false;
			}


			async UniTask Spin(bool x6)
			{
				airPart.spin.gameObject.SetActive(true);
				airPart.spin.SetTrigger(x6 ? AirPart.SPIN6 : AirPart.SPIN1);
				await UniTask.WaitWhile(() => airPart.spin.gameObject.activeSelf, cancellationToken: token);
				(partObj = airPart.parts[0].obj).SetActive(true);
			}
		}


		private async UniTask Fall()
		{
			var token = cancelAll.Token;
			if (token_Registered_AirMove != token)
				(token_Registered_AirMove = token).RegisterWithoutCaptureExecutionContext(StopAirMove);
			var v = new Vector3(runningPart.speed * (int)currentDirection.x, -airPart.fallSpeed);
			HideAllPartObjs();
			Vector3 pos, dest;
			float distance = 0;
			airPart.obj.SetActive(true);
			var partObj = airPart.parts[0].obj;
			partObj.SetActive(true);
			transform.parent = null;

			while ((dest = Tilemap.FindMoveDestination(this, v)).y < (pos = transform.position).y)
			{
				transform.position = dest;
				distance += pos.y - dest.y;
				if (distance > 1 && partObj != airPart.parts[2].obj)
				{
					partObj.SetActive(false);
					(partObj = airPart.parts[2].obj).SetActive(true);
				}

				currentDirection = default;
				await UniTask.Delay(15, delayTiming: PlayerLoopTiming.LastUpdate, cancellationToken: token);
				v.x = runningPart.speed * (int)currentDirection.x;
			}

			transform.position = dest;
			partObj.SetActive(false);
			airPart.parts[1].obj.SetActive(true);
			await UniTask.Delay(100, cancellationToken: token);
			airPart.parts[1].obj.SetActive(false);
			airPart.obj.SetActive(false);
			if (distance > 2)
			{
				sitPart.obj.SetActive(true);
				await UniTask.Delay(300, cancellationToken: token);
				sitPart.obj.SetActive(false);
			}

			if (currentDirection != default) ForceSetDirection(currentDirection);
			else idlePart.obj.SetActive(true);
		}


		private void StopAirMove()
		{
			for (int i = 0; i < airPart.parts.Length; ++i) airPart.parts[i].obj.SetActive(false);
			airPart.spin.gameObject.SetActive(false);
			token_Registered_AirMove = default;
		}
		#endregion


		#region Attack
		private enum AttackMode
		{
			Stand, Running, Air
		}

		[Serializable]
		private struct AttackingPart
		{
			public GameObject obj;

			[Serializable]
			public struct Data
			{
				public GameObject obj;
				public Part[] commons, extras;
			}
			public SerializableDictionaryBase<AttackMode, Data> data;
		}
		[SerializeField] private AttackingPart attackingPart;
		private UniTask taskAnimAttack;


		private async UniTask AnimAttack()
		{

		}
		#endregion


		#region Stealth
		[SerializeField] private Transform visibleBody;
		[Tooltip("MP tiêu hao khi tàng hình")]
		[SerializeField] private float mp_Stealth;
		private CancellationTokenSource cancelStealth;
		private bool ΔisStealth;

		private bool isStealth
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => ΔisStealth;

			set
			{
				if (taskDamage.isRunning() || value == ΔisStealth) return;
				if (!value)
				{
					cancelStealth.Cancel();
					cancelStealth.Dispose();
					ΔisStealth = false;
					return;
				}

				if (!skill.stealth || mp == 0) return;
				ΔisStealth = true;
				cancelStealth = new CancellationTokenSource();
				Stealth();


				async void Stealth()
				{
					visibleBody.localPosition = new Vector3(0, Tilemap.size.y);
					eyes.SetActive(true);
					var globalToken = cancelAll.Token;
					using var cts = CancellationTokenSource.CreateLinkedTokenSource(globalToken, cancelStealth.Token);
					var token = cts.Token;
					token.RegisterWithoutCaptureExecutionContext(Restore);

					do
					{
						mp -= mp_Stealth;
						await UniTask.Yield(PlayerLoopTiming.LastUpdate);
					} while (!token.IsCancellationRequested && mp > 0);
					Restore();


					void Restore()
					{
						visibleBody.localPosition = default;
						if (globalToken.IsCancellationRequested || !isUnderground) eyes.SetActive(false);
					}
				}
			}
		}
		#endregion


		#region Damage
		private CancellationTokenSource cancelAll = new CancellationTokenSource();
		private UniTask taskDamage;

		private async UniTask Damage()
		{

		}
		#endregion


		private void Awake()
		{
			airPart.spin.GetComponent<AnimEvent>().onEvent_Int += _ => airPart.spin.gameObject.SetActive(false);
		}


		// Test
		private void Update()
		{
			var k = Keyboard.current;
			var d = new Direction
			(
				x: k.leftArrowKey.isPressed || k.numpad7Key.isPressed || k.numpad4Key.isPressed || k.numpad1Key.isPressed ? Direction.Value.Negative
					: k.rightArrowKey.isPressed || k.numpad9Key.isPressed || k.numpad6Key.isPressed || k.numpad3Key.isPressed ? Direction.Value.Positive : 0,
				y: k.downArrowKey.isPressed || k.numpad1Key.isPressed || k.numpad2Key.isPressed || k.numpad3Key.isPressed ? Direction.Value.Negative
			: k.upArrowKey.isPressed || k.numpad7Key.isPressed || k.numpad8Key.isPressed || k.numpad9Key.isPressed ? Direction.Value.Positive : 0
			);

			if (d != default) direction = d;
		}
	}
}