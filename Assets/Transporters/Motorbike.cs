//using Cysharp.Threading.Tasks;
//using NinjaSchool.Platforms;
//using System;
//using System.Collections.Generic;
//using System.Linq.Expressions;
//using System.Runtime.CompilerServices;
//using System.Runtime.InteropServices.WindowsRuntime;
//using System.Threading;
//using UnityEngine;


//namespace NinjaSchool.Transporters
//{
//	public sealed class Motorbike : Transporter
//	{
//		[SerializeField] private GameObject off, on, up, stand;
//		private Ninja driver;


//		#region Lên xe
//		private void OnTriggerEnter2D(Collider2D collision)
//		{
//			var ninja = collision.GetComponent<Ninja>();
//			if (!ninja) return;

//			if (driver || ninja.transform.position.y <= transform.position.y) ;
//			//ninja.onDirectionChanged += (ninjaEvents[ninja] = new NinjaEvent(this, ninja)).Check;
//			else Use(ninja);
//		}


//		private readonly Dictionary<Ninja, NinjaEvent> ninjaEvents = new Dictionary<Ninja, NinjaEvent>();
//		private void OnTriggerExit2D(Collider2D collision)
//		{
//			var ninja = collision.GetComponent<Ninja>();
//			if (!ninja) return;

//			if (ninjaEvents.ContainsKey(ninja))
//			{
//				//ninja.onDirectionChanged -= ninjaEvents[ninja].Check;
//				ninjaEvents.Remove(ninja);
//			}
//		}


//		private sealed class NinjaEvent
//		{
//			private Ninja ninja;
//			private Motorbike motorbike;


//			[MethodImpl(MethodImplOptions.AggressiveInlining)]
//			public NinjaEvent(Motorbike motorbike, Ninja ninja)
//			{
//				this.motorbike = motorbike; this.ninja = ninja;
//			}


//			public void Check(Vector2 dir)
//			{
//				if (!motorbike.driver && dir.y > 0) motorbike.Use(ninja);
//			}
//		}


//		private void Use(Ninja ninja)
//		{
//			(driver = ninja).gameObject.SetActive(false);
//			off.SetActive(false);
//			on.SetActive(true);
//			UniTask.DelayFrame(10).ContinueWith(() => lockDirection = false).Forget();
//		}
//		#endregion


//		private bool _isRightFace = true;
//		private static readonly Quaternion EULER_Y180 = Quaternion.Euler(0, 180, 0);
//		private bool isRightFace
//		{
//			[MethodImpl(MethodImplOptions.AggressiveInlining)]
//			get => _isRightFace;

//			[MethodImpl(MethodImplOptions.AggressiveInlining)]
//			set => transform.rotation = (_isRightFace = value) ? Quaternion.identity : EULER_Y180;
//		}


//		private Vector2 currentDirection;
//		private bool lockDirection = true;
//		/// <summary>
//		/// Input 8 hướng (<see cref="Direction"/>): <br/>
//		/// Chạy, Nhảy
//		/// </summary>
//		public Vector2 direction
//		{
//			[MethodImpl(MethodImplOptions.AggressiveInlining)]
//			get => currentDirection;

//			set
//			{
//#if DEBUG
//				if (!value.IsValidDirection() || value == default) throw new Exception($"value không hợp lệ. value= {value}");
//#endif
//				if (lockDirection) return;
//				isRightFace = value.x > 0 || (value.x >= 0 && isRightFace);
//				switch (value.y)
//				{
//					case 0:
//						#region L, R
//						currentDirection = taskJump.Status == UniTaskStatus.Pending ? value + Direction.Up
//							: taskFall.Status == UniTaskStatus.Pending ? value + Direction.Down : value;
//						if (currentDirection == value && taskRun.Status != UniTaskStatus.Pending) taskRun = Run();
//						#endregion
//						break;

//					case 0.5f:
//						#region U, LU, RU
//						if (taskFall.Status == UniTaskStatus.Pending)
//						{
//							currentDirection.Set(value.x, -0.5f);
//							break;
//						}

//						if (taskJump.Status == UniTaskStatus.Pending)
//						{
//							currentDirection = value;
//							break;
//						}

//						if (taskRun.Status == UniTaskStatus.Pending)
//						{
//							Extensions.Cancel(ref cancelRun);
//							lockDirection = true;
//							taskRun.ContinueWith(() =>
//							{
//								taskRun = UniTask.CompletedTask;
//								currentDirection = value;
//								var dest = PhysicalPlatform.FindMoveTarget(transform.position, Direction.Down);
//								if (dest.y == transform.position.y) taskJump = Jump();
//								else
//								{
//									currentDirection.y = -0.5f;
//									taskFall = Fall();
//								}
//								lockDirection = false;
//							}).Forget();
//							break;
//						}

//						currentDirection = value;
//						taskJump = Jump();
//						#endregion
//						break;

//					case -0.5f:
//						#region D, LD, RD
//						if (taskJump.Status == UniTaskStatus.Pending) break;
//						if (taskRun.Status == UniTaskStatus.Pending)
//						{
//							if (value.x != 0) currentDirection.Set(value.x, 0);
//							break;
//						}
//						if (taskFall.Status == UniTaskStatus.Pending)
//						{
//							currentDirection = value;
//							break;
//						}

//						if (value.x == 0)
//						{
//							DriverExit().Forget();
//							break;
//						}

//						currentDirection.Set(value.x, 0);
//						taskRun = Run();
//						#endregion
//						break;

//					default: throw new Exception();
//				}
//			}
//		}


//		#region Run
//		[SerializeField] private float runSpeed;
//		[SerializeField] private int animRunDelay;
//		private CancellationTokenSource cancelRun = new CancellationTokenSource();
//		private UniTask taskRun = UniTask.CompletedTask;


//		/// <summary>
//		/// Nhớ gán <see cref="taskRun"/>
//		/// </summary>
//		private async UniTask Run()
//		{
//			DisableAll();
//			var cancelAnim = new CancellationTokenSource();
//			Vector2 lastDir = currentDirection;
//			using (var globalCts = CancellationTokenSource.CreateLinkedTokenSource(cancelRun.Token, cancelAllTask.Token))
//			{
//				var globalToken = globalCts.Token;
//				Anim().Forget();

//				try
//				{
//					do
//					{
//						var A = (Vector2)transform.position;
//						var B = PhysicalPlatform.FindMoveTarget(A, currentDirection);
//						var crossDir = currentDirection + Direction.Down;
//						if (currentDirection != lastDir)
//						{
//							lastDir = currentDirection;
//							Extensions.Cancel(ref cancelAnim);
//							Anim().Forget();
//						}
//						currentDirection = default;
//						if (B.x != A.x)
//						{
//							// A có thể đi trái/phải tới B
//							var C = PhysicalPlatform.FindMoveTarget(B, Direction.Down);
//							if (C.y != B.y)
//							{
//								// B có thể xuống C
//								var D = PhysicalPlatform.FindMoveTarget(A, crossDir);
//								if (D.x != A.x && D.y < A.y)
//								{
//									// A có thể đi tới D theo crossDir
//									currentDirection = crossDir;
//									taskFall = Fall();
//									goto CANCELED;
//								}
//								else
//								{
//									await transform.Move(B, runSpeed);
//									if (globalToken.IsCancellationRequested) goto CANCELED;
//									currentDirection = Direction.Down;
//									taskFall = Fall();
//									goto CANCELED;
//								}
//							}
//							else
//							{
//								await transform.Move(B, runSpeed);
//								if (globalToken.IsCancellationRequested) goto CANCELED;
//							}
//						}
//						else
//						{
//							// A không thể đi tới B
//							Extensions.Cancel(ref cancelAnim);
//							on.SetActive(false);
//							up.SetActive(true);
//							await UniTask.DelayFrame(1);
//							if (globalToken.IsCancellationRequested) goto CANCELED;

//							await UniTask.WaitUntil(() =>
//							{
//								if (globalToken.IsCancellationRequested || currentDirection == default || currentDirection != lastDir) return true;
//								currentDirection = default;
//								return false;
//							});
//							if (globalToken.IsCancellationRequested) goto CANCELED;
//							if (currentDirection != default) Anim().Forget();
//						}
//					} while (currentDirection.x != 0 && currentDirection.y == 0);

//					on.SetActive(true);
//					up.SetActive(false);
//					stand.SetActive(false);
//					CANCELED:;
//					Extensions.Cancel(ref cancelAnim);
//				}
//				finally { cancelAnim.Dispose(); }


//				async UniTask Anim()
//				{
//					using (var cts = CancellationTokenSource.CreateLinkedTokenSource(cancelAnim.Token, globalToken))
//					{
//						var token = cts.Token;
//						up.SetActive(true);
//						if (await UniTask.Delay(animRunDelay, cancellationToken: token).SuppressCancellationThrow()) return;
//						up.SetActive(false);
//						stand.SetActive(true);
//						if (await UniTask.Delay(animRunDelay, cancellationToken: token).SuppressCancellationThrow()) return;
//						stand.SetActive(false);
//						on.SetActive(true);

//						while (!token.IsCancellationRequested) await UniTask.Yield();
//					}
//				}
//			}
//		}
//		#endregion


//		[SerializeField] private float airSpeed;
//		[NonSerialized] public bool useInWater = true;


//		#region Jump
//		//[SerializeField] private AnimEvent upEvent;
//		//[SerializeField] private Animator upAnim;
//		private UniTask taskJump = UniTask.CompletedTask;


//		/// <summary>
//		/// Nhớ gán <see cref="taskJump"/>
//		/// </summary>
//		private async UniTask Jump()
//		{
//			var token = cancelAllTask.Token;
//			DisableAll();
//			up.SetActive(true);
//			int distance = 0;

//			while (true)
//			{
//				var dest = PhysicalPlatform.FindMoveTarget(transform.position, currentDirection);
//				if (dest.y == transform.position.y) break;

//				++distance;
//				await transform.Move(dest, airSpeed);
//				if (token.IsCancellationRequested) return;
//				if (distance == 4) break;
//			}

//			if (await UniTask.Delay(100, cancellationToken: token).SuppressCancellationThrow()) return;
//			up.SetActive(false);
//			currentDirection.y = -0.5f;
//			taskFall = Fall();
//		}
//		#endregion


//		#region Fall
//		private UniTask taskFall = UniTask.CompletedTask;


//		/// <summary>
//		/// Nhớ gán <see cref="taskFall"/>
//		/// </summary>
//		private async UniTask Fall()
//		{
//			var token = cancelAllTask.Token;
//			DisableAll();
//			on.SetActive(true);
//			while (true)
//			{
//				var pos = transform.position;
//				var dest = PhysicalPlatform.FindMoveTarget(pos, currentDirection);
//				currentDirection = Direction.Down;

//				if (!useInWater && Mathf.Floor(pos.y) != pos.y && dest.y != pos.y)
//				{
//					int a = Mathf.FloorToInt(pos.x), b = Mathf.CeilToInt(pos.x);
//					var p = PhysicalPlatform.array[Mathf.Abs(pos.x - a) < Mathf.Abs(pos.x - b) ? a : b][(int)(pos.y - 0.5f)] as Water;
//					if (p && p.isSurface)
//					{
//						// Chạm mặt nước từ trên rớt xuống
//						throw new NotImplementedException();
//						break;
//					}
//				}
//				if (dest.y == pos.y) break;

//				// Còn rơi xuống được
//				await transform.Move(dest, airSpeed);
//				if (token.IsCancellationRequested) return;
//			}

//			// Hiệu ứng tưng tưng
//		}
//		#endregion


//		[MethodImpl(MethodImplOptions.AggressiveInlining)]
//		private void DisableAll()
//		{
//			off.SetActive(false);
//			on.SetActive(false);
//			up.SetActive(false);
//			stand.SetActive(false);
//		}


//		private async UniTask<Ninja> DriverExit()
//		{
//			var token = cancelAllTask.Token;

//			// Hủy input

//			lockDirection = true;
//			//if (!driver.isReady) if (await UniTask.WaitUntil(() => driver.isReady, cancellationToken: token).SuppressCancellationThrow()) return null;
//			off.SetActive(true);
//			driver.transform.position = transform.position;
//			driver.gameObject.SetActive(true);
//			var ninja = driver;
//			driver = null;
//			return ninja;
//		}


//		public async UniTask DriverDie()
//		{
//			Extensions.Cancel(ref cancelAllTask);
//			DisableAll();
//			on.SetActive(true);
//			var ninja = await DriverExit();

//			//ninja. Die()
//			throw new NotImplementedException();
//		}




//		private void Update()
//		{
//			Vector2 d = default;
//			if (Input.GetKey(KeyCode.LeftArrow)) d = Direction.Left;
//			else if (Input.GetKey(KeyCode.RightArrow)) d = Direction.Right;

//			if (Input.GetKey(KeyCode.UpArrow)) d += Direction.Up;
//			else if (Input.GetKey(KeyCode.DownArrow)) d += Direction.Down;

//			if (d == default)
//			{
//				if (Input.GetKey(KeyCode.Keypad4)) d = Direction.Left;
//				else if (Input.GetKey(KeyCode.Keypad7)) d = Direction.Left + Direction.Up;
//				else if (Input.GetKey(KeyCode.Keypad8)) d = Direction.Up;
//				else if (Input.GetKey(KeyCode.Keypad9)) d = Direction.Right + Direction.Up;
//				else if (Input.GetKey(KeyCode.Keypad6)) d = Direction.Right;
//				else if (Input.GetKey(KeyCode.Keypad3)) d = Direction.Right + Direction.Down;
//				else if (Input.GetKey(KeyCode.Keypad2)) d = Direction.Down;
//				else if (Input.GetKey(KeyCode.Keypad1)) d = Direction.Left + Direction.Down;
//			}
//			if (d != default) direction = d;
//		}
//	}
//}