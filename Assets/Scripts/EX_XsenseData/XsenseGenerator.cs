using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;

public class XsenseGenerator : playEnvMotion
{
	public Actor _actor_target;
	public Actor _actor_output;
	public WriterClass _writer_class;
	private StringBuilder sb_record;
	public string srcTxtFileName;
	public string tarTxtFileName;
	public bool b_connect = false;
	public bool b_data = false;
	public bool b_goal = false;
	public Matrix4x4 pre_mat;
	public Matrix4x4 cur_mat;
	public Vector3 cur_root_vel;
	float[,] joints_pose;
	public bool b_goal_data_exist = false;
	public HelloRequesterB _helloRequester_output;
	// Start is called before the first frame update
	public Quaternion SET_JOINT_QUATERNION_MW(Quaternion input_Quat)
	{
		Quaternion quat_rH = new Quaternion(input_Quat.x, input_Quat.y * -1.0f, input_Quat.z * -1.0f, input_Quat.w);

		return quat_rH;
	}
	public bool extract_Desired(Actor _actor, float y_offset, out Vector3 root_velocity, out float[] occupancies, out float[,] joints_position)
    {
		cur_mat = base.GetRootTransformation_JointTransformation(_actor, y_offset);
		//
		root_velocity = base.GetRootVelocity(pre_mat, cur_mat);
		pre_mat = cur_mat;
		//
		Environment.Sense(cur_mat, LayerMask.GetMask("Default", "Interaction"));
		occupancies = Environment.Occupancies;
		// current root 에서 본 pose 얻기
		joints_position = new float[_actor.Bones.Length, 3];
		for (int j = 0; j < _actor.Bones.Length; j++)
		{
			Vector3 position_j = _actor.Bones[j].Transform.position.GetRelativePositionTo(cur_mat);
			joints_position[j, 0] = position_j.x;
			joints_position[j, 1] = position_j.y;
			joints_position[j, 2] = position_j.z;
		}

		return true;
	}
	private void write_goal_feature(Actor _actor, float[,] joint_position, Vector3 root_vel, int b_upper_cond)
	{
		sb_record = new StringBuilder();
		//sb_record.Append(Frame);
		sb_record = WriteFloat(sb_record, b_upper_cond, true);
		// joint position
		for (int j = 0; j < _actor.Bones.Length; j++)
		{
			if (j == 0)
				sb_record = WritePosition(sb_record, new Vector3(joint_position[j, 0], joint_position[j, 1], joint_position[j, 2]), false);
			else
				sb_record = WritePosition(sb_record, new Vector3(joint_position[j, 0], joint_position[j, 1], joint_position[j, 2]), false);
		}
		// Environment Sensor
		for (int e = 0; e < Environment.Occupancies.Length; e++)
			sb_record = WriteFloat(sb_record, Environment.Occupancies[e], false);
		// root velocity
		sb_record = WritePosition(sb_record, root_vel, false);

	}
	public void event_WriteGoalData()
	{
		string foldername = directoryPath;
		string name = "SeqDemo";

		base.File_record = _writer_class.CreateFile(foldername, name, true, ".txt");

		b_record = true;
		b_play = true;
		//b_output_data_exist = false;

		FileIndex = 0;
		FileOrder = findOrder(Files, FileIndex);
		Frame = 1;

		start_root_frame = Files[FileOrder].RootTr[Frame];
		io_class.WriteMatData(foldername, name + "_start_root", start_root_frame);
		Debug.Log("let's b_upper_cond " + Files[FileOrder].b_uppercond);
	}
	public void event_LoadGoalData()
	{
		b_data_exist = false;
		b_goal_data_exist = true;
		
		string foldername = directoryPath;
		string name = "SeqDemo";

		Debug.Log("loadGoal: " + io_class.ImportTextGoalData(foldername, name));

		Debug.Log("Goal Frames: " + io_class.Goals.Length);

		Debug.Log("Goal start Root: " + io_class.ImportTextRootData(foldername, name + "_start_root"));

		Vector3 position = new Vector3(io_class.RootMat[0][0], io_class.RootMat[0][1], io_class.RootMat[0][2]);
		Quaternion quat = new Quaternion(io_class.RootMat[0][3], io_class.RootMat[0][4], io_class.RootMat[0][5], io_class.RootMat[0][6]);
		start_root_frame.SetTRS(position, quat, Vector3.one);

		//write result
		File_record = _writer_class.CreateFile(foldername, name, true, "output.csv");
		//
		//File_record_prob = _writer_class.CreateFile(foldername, name, true, "output_prob.csv");

		b_record = true;

		Frame = 0;
	}
	/*
	 you should define those child functions
	 */
	protected override void Setup_Child()
	{
		pre_mat = Matrix4x4.identity;
		// connection initialization
		_helloRequester_output = new HelloRequesterB();
		_helloRequester_output.Start(); //Thread 실행

	}
	protected override void Feed_Child()
	{
		// 
		if (b_goal_data_exist)
		{
			Debug.Log("Frame " + Frame);
			if (Frame == (io_class.Goals.Length))
			{
				Frame = 0;
				if (b_record)
				{
					Debug.Log("write finish: ");
					File_record.Close();
					//File_record_prob.Close();
					b_record = false;
				}
			}

			//Debug.Log("Frame " + Frame + "/" + io_class.Goals.Length + "" + io_class.Goals[Frame][3 * 0 + 1]);
			if (b_connect)
			{
				Debug.Log("Frame " + Frame + "/" + io_class.Goals.Length + "" + io_class.Goals[Frame][3 * 0 + 1]);
				// String Builder 를 이용해서, 값을 넣는다.
				sb_record = new StringBuilder();
				sb_record.Append(Frame);

				for (int p = 0; p < io_class.Goals[Frame].Length; p++)
				{
					WriteFloat(sb_record, io_class.Goals[Frame][p], false);
				}

				//send
				// sb_input 을 python 으로 보낸다.
				string st_pos = sb_record.ToString();

				_helloRequester.bool_RecvComplete = true;
				_helloRequester.bool_SendComplete = true;
				_helloRequester.str_message_send = st_pos;
				sb_record.Clear();


			}

			Frame = Frame + 1;
		}
		else
		{
			// character retargeting
			if (b_connect)
			{
				//Debug.Log("connected");

				// String Builder 를 이용해서, 값을 넣는다.
				sb_record = new StringBuilder();
				sb_record.Append(Frame);

				for (int p = 0; p < _actor.Bones.Length; p++)
				{
					if (p == 0)
						sb_record = WritePosition(sb_record, _actor.Bones[p].Transform.position, false);

					Quaternion quat = _actor.Bones[p].Transform.localRotation;
					quat = quat.GetNormalised();
					sb_record = WriteQuat(sb_record, quat, false);
				}

				//send
				// sb_input 을 python 으로 보낸다.
				string st_pos = sb_record.ToString();

				_helloRequester.bool_RecvComplete = true;
				_helloRequester.bool_SendComplete = true;
				_helloRequester.str_message_send = st_pos;
				sb_record.Clear();

			}
			// extract goal data
			if (b_connect && _helloRequester.bool_SendComplete == true && _helloRequester.bool_RecvComplete == true)
			{

				string message = _helloRequester.DataPostProcessing();//
				io_class.ImportConnectionData(message, _actor_target, start_root_frame, io_class.scale); //

				// extract goal input
				float[] env_occ;
				b_goal = extract_Desired(_actor_target, 0.0f, out cur_root_vel, out env_occ, out joints_pose);

				// send goal data
				if (b_goal && base.b_record == false && b_data == false)
				{
					_helloRequester.bool_RecvComplete = false;
					_helloRequester.bool_SendComplete = false;
					// String Builder 를 이용해서, 값을 넣는다.
					write_goal_feature(_actor_target, joints_pose, cur_root_vel, 1);
					// sb_input 을 python 으로 보낸다.
					string st_pos = sb_record.ToString();

					_helloRequester_output.bool_RecvComplete = true;
					_helloRequester_output.bool_SendComplete = true;
					_helloRequester_output.str_message_send = st_pos;
					sb_record.Clear();

					if (_helloRequester_output.bool_SendComplete == true && _helloRequester_output.bool_RecvComplete == true)
					{
						message = _helloRequester_output.DataPostProcessing();//
						io_class.ImportConnectionData(message, _actor_output, start_root_frame, io_class.scale); //

						Matrix4x4 output_root = GetRootTransformation_JointTransformation(_actor_output, 0.0f);
						for (int j = 0; j < _actor_output.Bones.Length; j++)
						{
							if (j == 0)
								_actor_output.Bones[j].Transform.position = _actor_output.Bones[j].Transform.position.GetRelativePositionTo(output_root).GetRelativePositionFrom(cur_mat);

						}

					}

				}
				else if (b_goal && base.b_record == true && b_data == true)
				{
					// record goal data
					write_goal_feature(_actor_target, joints_pose, cur_root_vel, base.Files[base.FileOrder].b_uppercond);
					base.File_record.WriteLine(sb_record.ToString());
					sb_record.Clear();
				}
			}
		
		}

		

		



	}
	protected override void Read_Child()
	{
		
	}
	protected override void OnRender_Child()
	{
		if (b_goal)
		{
			UltiDraw.Begin();
			//Debug.Log("cur root vel" + cur_root_vel.ToString());
			Vector3 root_vel = new Vector3(cur_root_vel.x, 0.0f, cur_root_vel.y);
			root_vel = root_vel.GetRelativeDirectionFrom(pre_mat);
			UltiDraw.DrawLine(pre_mat.GetPosition(), pre_mat.GetPosition() + 10.25f * root_vel, 0.025f, 0f, UltiDraw.DarkGreen.Transparent(0.85f));
			UltiDraw.DrawWiredSphere(pre_mat, 0.1f, Color.blue, Color.red);
			UltiDraw.DrawTranslateGizmo(pre_mat.GetPosition(), pre_mat.GetRotation(), 0.1f);

			//
			Vector3 head = new Vector3(joints_pose[5, 0], joints_pose[5, 1], joints_pose[5, 2]);
			head = head.GetRelativePositionFrom(cur_mat);
			UltiDraw.DrawWiredSphere(head, Matrix4x4.identity.GetRotation(), 0.1f, Color.red, Color.black);

			Vector3 lhand = new Vector3(joints_pose[9, 0], joints_pose[9, 1], joints_pose[9, 2]);
			lhand = lhand.GetRelativePositionFrom(cur_mat);
			UltiDraw.DrawWiredSphere(lhand, Matrix4x4.identity.GetRotation(), 0.1f, Color.red, Color.black);

			Vector3 rhand = new Vector3(joints_pose[13, 0], joints_pose[13, 1], joints_pose[13, 2]);
			rhand = rhand.GetRelativePositionFrom(cur_mat);
			UltiDraw.DrawWiredSphere(rhand, Matrix4x4.identity.GetRotation(), 0.1f, Color.red, Color.black);


			UltiDraw.End();
		}
	}

	//
	[SerializeField] private Actor ActorR = null;
	public void SetTargetCharacter(Actor character)
	{
		if (_actor_target == null && character != null)
		{
			if (ActorR != null)
			{
				Utility.Destroy(ActorR.gameObject);
				ActorR = null;
			}
			_actor_target = character;
		}
		else
		{
			_actor_target = character;
		}
	}
	public void SetOutputCharacter(Actor character)
	{
		if (_actor_output == null && character != null)
		{
			if (ActorR != null)
			{
				Utility.Destroy(ActorR.gameObject);
				ActorR = null;
			}
			_actor_output = character;
		}
		else
		{
			_actor_output = character;
		}
	}

	[CustomEditor(typeof(XsenseGenerator), true)]
	public class XsenseGenerator_Editor : playEnvMotion_Editor
	{

		public XsenseGenerator Target_R;
		private bool ShowA;

		public new void Awake()
		{
			base.Awake();
			Target_R = (XsenseGenerator)target;
			Target_R._writer_class = new WriterClass();
		}
		public override void OnInspectorGUI()
		{
			ShowA = EditorGUILayout.BeginFoldoutHeaderGroup(ShowA, "Play Source Data");
			if (ShowA)
				base.OnInspectorGUI();
			EditorGUILayout.EndFoldoutHeaderGroup();

			EditorGUILayout.LabelField("I'm a Label From Class retargeting");
			Inspector();

			
		}

		public new void Inspector()
		{
			Utility.ResetGUIColor();
			Utility.SetGUIColor(UltiDraw.LightGrey);
			using (new EditorGUILayout.VerticalScope("Box"))
			{
				Target_R.SetTargetCharacter((Actor)EditorGUILayout.ObjectField("Target Character", Target_R._actor_target, typeof(Actor), true));

				Target_R.SetOutputCharacter((Actor)EditorGUILayout.ObjectField("Output Character", Target_R._actor_output, typeof(Actor), true));
			}

			using (new EditorGUILayout.VerticalScope("Box"))
			{
				EditorGUILayout.BeginHorizontal();
				GUILayout.FlexibleSpace();  // 고정된 여백을 넣습니다.
											//EditorGUILayout.LabelField("b_connect", GUILayout.Width(100f));
				Target_R.b_connect = EditorGUILayout.Toggle("b_connect", Target_R.b_connect);
				GUILayout.FlexibleSpace();
				Target_R.b_data = EditorGUILayout.Toggle("b_data", Target_R.b_data);

				EditorGUILayout.EndHorizontal();

				//
				if (Utility.GUIButton("offline goal data", UltiDraw.DarkGrey, UltiDraw.White))
				{
					Target_R.event_WriteGoalData();
				}
				//
				if (Utility.GUIButton("offline motion generation", UltiDraw.DarkGrey, UltiDraw.White))
				{
					Target_R.event_LoadGoalData();
				}

			}

		}


		

	}

}
