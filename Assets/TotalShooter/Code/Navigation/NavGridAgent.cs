using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Sadalmalik.GridNavigation
{
	public enum NavGridAgentState
	{
		Idle = 0, FollowPath = 1, FollowDestination = 2
	}

	[RequireComponent(typeof(CharacterController))]
	public class NavGridAgent : MonoBehaviour
	{
		private CharacterController _controller;

		private Queue<NavGridNode> _path        = null;
		private Vector3?           _destination = null;

		public float stoppingDistance = 0.5f;

		public float gravity = -9.8f;

		public float speed = 0;


		private Vector3 _velocity;

		public Vector3 Velocity
		{
			get => _velocity;
			set => _velocity = value;
		}

		public NavGridAgentState State;

		public NavGridNode CurrentNode;

		public bool Grounded { get; private set; }


		void Awake()
		{
			NavGrid.Refresh();
			
			_controller = GetComponent<CharacterController>();

			CurrentNode = NavGrid.GetNearestNode(transform.position);
		}

		void Update()
		{
			DoGravity();
			
			switch (State)
			{
				case NavGridAgentState.Idle:
				{
					// Nothing to do here
					break;
				}

				case NavGridAgentState.FollowPath:
				{
					var delta = CurrentNode.position - transform.position;
					delta.y = 0;
					var magnitude = delta.magnitude;

					if (magnitude > CurrentNode.radius)
					{
						DoMove(delta / magnitude);
					}
					else if (_path.Count > 0)
					{
						CurrentNode = _path.Dequeue();
					}
					else
					{
						State = NavGridAgentState.FollowDestination;
					}
					break;
				}

				case NavGridAgentState.FollowDestination:
				{
					var delta = _destination.Value - transform.position;
					delta.y = 0;
					var magnitude = delta.magnitude;

					if (magnitude > stoppingDistance)
					{
						DoMove(delta / magnitude);
					}
					else
					{
						State        = NavGridAgentState.Idle;
						_path        = null;
						_destination = null;
					}

					break;
				}
			}
		}

		private void DoMove(Vector3 direction)
		{
			_controller.Move(direction * speed * Time.deltaTime);

			if (direction != Vector3.zero)
				gameObject.transform.forward = direction;
		}
		
		private void DoGravity()
		{
			Grounded = _controller.isGrounded;
			if (Grounded && _velocity.y < 0)
				_velocity.y = 0f;
			
			_velocity.y += gravity * Time.deltaTime;
			_controller.Move(_velocity * Time.deltaTime);
		}

		public void SetDestination(Vector3 position)
		{
			_destination = position;

			var endNode = NavGrid.GetNearestNode(position);

			var path = NavGrid.FindPath(CurrentNode, endNode);

			_path = new Queue<NavGridNode>(path);

			State = NavGridAgentState.FollowPath;

#if UNITY_EDITOR
			DumpPath(path);
#endif
		}

#if UNITY_EDITOR

		private void DumpPath(List<NavGridNode> path)
		{
			Debug.DrawLine(transform.position, _destination.Value);
			Debug.Log($"Found path: {path.Count} nodes");

			NavGridNode prev = null;
			foreach (var node in path)
			{
				if (prev != null)
					Debug.DrawLine(prev.position + Vector3.up, node.position + Vector3.up, Color.cyan, 15);
				prev = node;
			}

			if (prev!=null)
				Debug.DrawLine(prev.position + Vector3.up, _destination.Value + Vector3.up, Color.cyan, 15);
		}

#endif
	}
}