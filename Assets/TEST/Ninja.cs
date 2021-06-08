using Cysharp.Threading.Tasks;
using NinjaSchool;
using RotaryHeart.Lib.SerializableDictionary;
using System;
using System.Runtime.CompilerServices;
using System.Threading;
using UnityEngine;
using UnityEngine.InputSystem;


namespace hehe
{
	[DisallowMultipleComponent]
	public class Ninja : MonoBehaviour
	{
		private Direction currentDirection;
		private bool @lock;
		public Direction direction
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => currentDirection;

			set
			{
#if DEBUG
				if (value == default) throw new ArgumentOutOfRangeException();
#endif
				if (@lock) return;
				currentDirection = value;
				isRightFace = value.x > 0 || (value.x >= 0 && isRightFace);
				if (value.x != 0 && value.y == 0 && !taskRun.isRunning()) Run();
			}
		}


		[Serializable]
		private struct Part
		{
			public GameObject obj;
			public SpriteRenderer head;
		}
		[SerializeField] private Part idlePart, sittingPart;


		private void HideAllPartObjs()
		{
			idlePart.obj.SetActive(false);
			sittingPart.obj.SetActive(false);
			runningPart.obj.SetActive(false);
			airPart.obj.SetActive(false);
			attackingPart.obj.SetActive(false);
		}


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


		[Serializable]
		public struct Skill
		{
			[Tooltip("Khinh công nhảy cao: nhảy lên tối đa 3 ô")]
			public bool jumpHigh;

			[Tooltip("Kinh công nhảy siêu cao: nhảy lên tối đa 4 ô")]
			public bool jumpSuperHigh;

			[Tooltip("Độn thổ")]
			public bool underGround;

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
		[NonSerialized] public Skill skill;


		private float ΔHP;
		public float HP
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => ΔHP;

			set
			{
				ΔHP = value >= 0 ? value : 0;
			}
		}


		private float ΔMP;
		public float MP
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => ΔMP;

			set
			{
				ΔMP = value >= 0 ? value : 0;
			}
		}


		[SerializeField] private GameObject eyes;

		#region Sit, Underground
		private UniTask taskSit, taskUnderground;
		[Tooltip("Ngồi bao lâu mới có thể độn thổ ?")]
		[SerializeField] private float delayUndergroundSeconds;
		[Tooltip("MP tiêu hao khi độn thổ")]
		[SerializeField] private float MP_Underground;

		public bool isUnderground => taskUnderground.isRunning();


		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void Sit()
		{
			(taskSit = Sit()).Forget();


			async UniTask Sit()
			{
				var token = cancelAll.Token;
				HideAllPartObjs();
				sittingPart.obj.SetActive(true);
				float t = Time.time + delayUndergroundSeconds;

				do
				{
					if (skill.underGround && MP > 0 && Time.time > t
						/*&& địa hình cho phép độn thổ*/)
					{
						CleanupSit();
						(taskUnderground = Underground()).Forget();
						return;
					}

					currentDirection = default;
					await UniTask.NextFrame(PlayerLoopTiming.LastUpdate, token);
				} while (currentDirection.y < 0);
				CleanupSit();
				idlePart.obj.SetActive(true);


				void CleanupSit()
				{
					sittingPart.obj.SetActive(false);
					currentDirection = default;
				}


				async UniTask Underground()
				{
					transform.position += Vector3.down;
					eyes.SetActive(true);
					token.RegisterWithoutCaptureExecutionContext(Cleanup);

					do
					{
						MP -= MP_Underground;
						currentDirection = default;
						await UniTask.NextFrame(PlayerLoopTiming.LastUpdate, token);
						if (currentDirection != default)
							isRightFace = currentDirection.x > 0 || (currentDirection.x >= 0 && isRightFace);
					} while (MP > 0 && currentDirection.y <= 0);
					Cleanup();
					Jump();


					void Cleanup()
					{
						transform.position += Vector3.up;
						if (token.IsCancellationRequested || !isStealth) eyes.SetActive(false);
					}
				}
			}
		}
		#endregion


		#region Stealth
		private CancellationTokenSource cancelStealth;
		[SerializeField] private Transform visibleParts;
		[Tooltip("MP tiêu hao khi tàng hình")]
		[SerializeField] private float MP_Stealth;
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

				if (!skill.stealth || MP == 0) return;
				ΔisStealth = true;
				cancelStealth = new CancellationTokenSource();
				Stealth();


				async void Stealth()
				{
					//visibleParts.localPosition = new Vector3(0, Tile.arraySize.y);
					eyes.SetActive(true);
					var globalToken = cancelAll.Token;
					using (var cts = CancellationTokenSource.CreateLinkedTokenSource(globalToken, cancelStealth.Token))
					{
						var token = cts.Token;
						token.RegisterWithoutCaptureExecutionContext(Cleanup);

						do
						{
							MP -= MP_Stealth;
							await UniTask.Yield(PlayerLoopTiming.LastUpdate);
						} while (!token.IsCancellationRequested && MP > 0);
						Cleanup();
					}


					void Cleanup()
					{
						visibleParts.localPosition = default;
						if (globalToken.IsCancellationRequested || !taskUnderground.isRunning())
							eyes.SetActive(false);
					}
				}
			}
		}
		#endregion


		#region Run
		[Serializable]
		private struct RunningPart
		{
			public GameObject obj;
			public Part[] parts;
			[Tooltip("moveDelta > 0")]
			public float moveDelta;
			public int moveDelayMilisec, animDelayMilisec;
		}
		[SerializeField] private RunningPart runningPart;
		private UniTask taskRun;
		private CancellationTokenSource cancelRun = new CancellationTokenSource();


		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void Run()
		{
			(taskRun = Run()).Forget();


			async UniTask Run()
			{
				if (taskRun.Status == UniTaskStatus.Canceled)
					foreach (var p in runningPart.parts) p.obj.SetActive(false);

				HideAllPartObjs();
				runningPart.obj.SetActive(true);
				using (var cts = CancellationTokenSource.CreateLinkedTokenSource(cancelRun.Token, cancelAll.Token))
				{
					var token = cts.Token;
					var moveVector = new Vector3(runningPart.moveDelta * (currentDirection.x > 0 ? 1 : -1), 0);
					await UniTask.WhenAll(Anim(), Move());


					async UniTask Anim()
					{
						var lastDir = currentDirection;
						while (true)
						{
							for (int i = 0; i < 5; ++i)
							{
								var obj = runningPart.parts[i].obj;
								obj.SetActive(true);
								currentDirection = default;
								await UniTask.Delay(runningPart.animDelayMilisec, delayTiming: PlayerLoopTiming.LastUpdate, cancellationToken: token);
								obj.SetActive(false);
								if ((i == 0 || i == 2) && currentDirection == default)
								{
									cancelRun.Cancel();
									cancelRun.Dispose();
									cancelRun = new CancellationTokenSource();
									runningPart.obj.SetActive(false);
									idlePart.obj.SetActive(true);
									return;
								}

								if (currentDirection != default && currentDirection != lastDir)
								{
									lastDir = currentDirection;
									moveVector *= -1;
								}
							}
						}
					}


					async UniTask Move()
					{
						while (!await UniTask.Delay(runningPart.moveDelayMilisec, cancellationToken: token).SuppressCancellationThrow())
							transform.position += moveVector;
					}
				}
			}
		}
		#endregion


		#region Air: Jump, Fall
		[Serializable]
		private struct AirPart
		{
			public GameObject obj;
			public Part[] parts;

			[Tooltip("Dùng để tính vector nhảy/ rơi theo trục Y. vy= (0; +/-0.5) / vectorYCount")]
			public int vectorYCount;

			[Tooltip("Thời gian delay khi nhảy/ rơi theo vector v = vx + vy")]
			public int moveDelayMilisec;
			public static readonly int SPIN1 = Animator.StringToHash("SPIN1"), SPIN6 = Animator.StringToHash("SPIN6");
			public Animator spinAnim;
			public int delayMilisecAfterJumping;

			[Tooltip("MP tiêu hao khi sử dụng khinh công để nhảy cao/siêu cao")]
			public float MP_ExtraJump;


			public void Awake()
			{
				var @this = this;
				spinAnim.GetComponent<AnimEvent>().onEvent_String += (s) => @this.spinAnim.gameObject.SetActive(false);
			}
		}
		[SerializeField] private AirPart airPart;


		private UniTask taskJump;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void Jump()
		{
			(taskJump = Jump()).Forget();


			async UniTask Jump()
			{
				var p = airPart.parts;
				if (taskJump.Status == UniTaskStatus.Canceled || taskFall.Status == UniTaskStatus.Canceled)
				{
					foreach (var pp in p) pp.obj.SetActive(false);
					airPart.spinAnim.gameObject.SetActive(false);
				}

				HideAllPartObjs();
				airPart.obj.SetActive(true);
				p[1].obj.SetActive(true);
				airMoveVector = default;
				airLastDir = default;
				var token = cancelAll.Token;

				// Move 1.5y
				for (int i = 0; i < 3; ++i) await AirMove(true, token);

				if (!skill.jumpHigh)
				{
					// Nhảy thấp khi chưa học khinh công
					await UniTask.Delay(airPart.delayMilisecAfterJumping, cancellationToken: token);
					p[1].obj.SetActive(false);
					airPart.obj.SetActive(false);
					Fall();
					return;
				}

				// reset input
				currentDirection = default;

				// Move 0.5y
				await AirMove(true, token);

				UniTask spinTask;
				if (MP == 0 || currentDirection.y <= 0)
				{
					// Nhảy bình thường
					p[1].obj.SetActive(false);
					spinTask = Spin(true);

					// Xử lý L/R
					if (currentDirection == Direction.left || currentDirection == Direction.right)
						await UniTask.WhenAll(spinTask,
							transform.Move_deprecated(transform.position + new Vector3(runningPart.moveDelta * 60 * (currentDirection == Direction.right ? 1 : -1), 0), runningPart.moveDelta, token, runningPart.moveDelayMilisec));
					else await spinTask;

					p[0].obj.SetActive(false);
					airPart.obj.SetActive(false);
					Fall();
					return;
				}

				MP -= airPart.MP_ExtraJump;
				p[1].obj.SetActive(false);
				await Spin(false);

				// Move 1y
				await AirMove(true, token);
				// reset input
				currentDirection = default;
				await AirMove(true, token);

				if (!skill.jumpSuperHigh || MP == 0 || currentDirection.y <= 0)
				{
					// Nhảy cao
					p[0].obj.SetActive(false);
					spinTask = Spin(true);

					// Xử lý L/R
					if (currentDirection == Direction.left || currentDirection == Direction.right)
						await UniTask.WhenAll(spinTask,
							transform.Move_deprecated(transform.position + new Vector3(runningPart.moveDelta * 60 * (currentDirection == Direction.right ? 1 : -1), 0), runningPart.moveDelta, token, runningPart.moveDelayMilisec));
					else await spinTask;

					p[0].obj.SetActive(false);
					airPart.obj.SetActive(false);
					Fall();
					return;
				}

				p[0].obj.SetActive(false);
				await Spin(false);

				// Move 1y
				await AirMove(true, token);
				await AirMove(true, token);

				// Nhảy siêu cao
				p[0].obj.SetActive(false);
				spinTask = Spin(true);

				// Xử lý L/R
				if (currentDirection == Direction.left || currentDirection == Direction.right)
					await UniTask.WhenAll(spinTask,
						transform.Move_deprecated(transform.position + new Vector3(runningPart.moveDelta * 60 * (currentDirection == Direction.right ? 1 : -1), 0), runningPart.moveDelta, token, runningPart.moveDelayMilisec));
				else await spinTask;

				p[0].obj.SetActive(false);
				airPart.obj.SetActive(false);
				Fall();


				async UniTask Spin(bool x6)
				{
					airPart.spinAnim.gameObject.SetActive(true);
					airPart.spinAnim.SetTrigger(x6 ? AirPart.SPIN6 : AirPart.SPIN1);
					await UniTask.WaitWhile(() => airPart.spinAnim.gameObject.activeSelf, cancellationToken: token);
					p[0].obj.SetActive(true);
				}
			}
		}


		private UniTask taskFall;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void Fall()
		{
			(taskFall = Fall()).Forget();


			async UniTask Fall()
			{
				var p = airPart.parts;
				if (taskJump.Status == UniTaskStatus.Canceled || taskFall.Status == UniTaskStatus.Canceled)
				{
					foreach (var pp in p) pp.obj.SetActive(false);
					airPart.spinAnim.gameObject.SetActive(false);
				}

				HideAllPartObjs();
				airPart.obj.SetActive(true);
				airMoveVector = default;
				airLastDir = default;
				currentDirection.y = Direction.downValue;
				var token = cancelAll.Token;

				// Bật p0 -> rơi
				// nếu rơi quãng đường =1 và chưa chạm đất thì tắt p0, bật p2
				// nếu chạm đất thì tắt lastPart, bật p1
				// Tắt p1, nếu quãng đường rơi > 2 thì bật sit và đợi rồi tắt sit
				// tắt airPart -> idle


				// Test
				p[0].obj.SetActive(true);
				await AirMove(false, token);
				await AirMove(false, token);

				p[0].obj.SetActive(false);
				p[2].obj.SetActive(true);

				for (int i = 0; i < 4; ++i) await AirMove(false, token);

				p[2].obj.SetActive(false);
				p[1].obj.SetActive(true);
				await UniTask.Delay(1000, cancellationToken: token);
				p[1].obj.SetActive(false);

				sittingPart.obj.SetActive(true);
				await UniTask.Delay(1000, cancellationToken: token);
				sittingPart.obj.SetActive(false);
				airPart.obj.SetActive(false);
				idlePart.obj.SetActive(true);
			}
		}


		private Vector3 airMoveVector;
		private Direction airLastDir = default;

		/// <summary>
		/// Nhảy/ rơi 1 bước với  vector quãng đường = (0, +/-0.5) hoặc (+/-<see cref="RunningPart.moveDelta"/>, +/-0.5)
		///<para>Cập nhật <see cref= "airMoveVector" /> và <see cref="airLastDir"/></para>
		///<para>Trước khi nhảy hay rơi nhớ reset <see cref= "airMoveVector" /> và <see cref="airLastDir"/></para>
		/// </summary>
		/// <param name="isJumping"><see langword="true"/>: Đang nhảy, <see langword="false"/>: đang rơi</param>
		private async UniTask AirMove(bool isJumping, CancellationToken token)
		{
			float stopY = transform.position.y + (isJumping ? 0.5f : -0.5f);

			do
			{
				if (currentDirection != default && currentDirection != airLastDir)
				{
					#region Cập nhật {v} và {lastDir}
					airMoveVector = (airLastDir = currentDirection).ToVector2(runningPart.moveDelta, 0.5f / airPart.vectorYCount);
					airMoveVector.y = currentDirection == Direction.left || currentDirection == Direction.right ?
						(isJumping ? 0.5f : -0.5f) / airPart.vectorYCount
						: airMoveVector.y;
					#endregion
				}

				transform.position += airMoveVector;
				if (!isJumping)
				{
					airMoveVector.x = 0;
					airLastDir = Direction.down;
				}

				await UniTask.Delay(airPart.moveDelayMilisec, delayTiming: PlayerLoopTiming.LastUpdate, cancellationToken: token);
			} while (isJumping ? transform.position.y < stopY : transform.position.y > stopY);
			transform.position = new Vector3(transform.position.x, stopY);
		}
		#endregion


		#region Damage
		private UniTask taskDamage;
		private CancellationTokenSource cancelAll;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void Damage()
		{
			(taskDamage = Damage()).Forget();


			async UniTask Damage()
			{
				throw new NotImplementedException();
			}
		}
		#endregion


		#region Attack
		public enum AttackMode
		{
			Static, Running, Air
		}

		[Serializable]
		private struct AttackingPart
		{
			[Serializable]
			public struct Data
			{
				public GameObject obj;
				public Part[] commons, extras;
			}

			public GameObject obj;
			public SerializableDictionaryBase<AttackMode, Data> data;
			public Sword sword;   // debug
		}
		[SerializeField] private AttackingPart attackingPart;


		private bool attackPressed;
		private void PressAttackButton()
		{
			if (!taskAnimAttack.isRunning()) AnimAttack(AttackMode.Static);
			else attackPressed = true;
		}


		private UniTask taskAnimAttack;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void AnimAttack(AttackMode mode)
		{
			(taskAnimAttack = AnimAttack()).Forget();


			async UniTask AnimAttack()
			{
				attackPressed = false;
				throw new NotImplementedException();
			}
		}









		#endregion



		protected void Awake()
		{
			airPart.Awake();

			// debug
			var t = attackingPart.sword.transform;
			t.parent = transform;
			t.localPosition = default;

			skill.underGround = skill.stealth = true;
			HP = MP = 1000;
		}


		protected void OnEnable()
		{
			cancelAll = new CancellationTokenSource();
		}


		protected void OnDisable()
		{
			cancelAll.Cancel();
			cancelAll.Dispose();
		}



		// TEST
		private void Update()
		{
			var k = Keyboard.current;
			var d = new Direction
			(
				k.leftArrowKey.isPressed ? Direction.leftValue : k.rightArrowKey.isPressed ? Direction.rightValue : 0,
				k.upArrowKey.isPressed ? Direction.upValue : k.downArrowKey.isPressed ? Direction.downValue : 0
			);
			if (d != default) direction = d;


			if (k.enterKey.wasPressedThisFrame) PressAttackButton();
			if (k.spaceKey.wasPressedThisFrame) isStealth = !isStealth;
		}
	}
}