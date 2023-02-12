using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;

public class doRetargeting : playEnvMotion
{
    public Actor _actor_target;
	public WriterClass _writer_class;
	private StreamWriter File_record;
	private StringBuilder sb_record;
	public string srcTxtFileName;
	public string tarTxtFileName;
	public bool b_connect = false;
	public bool b_data = false;

	// Start is called before the first frame update
	public Quaternion SET_JOINT_QUATERNION_MW(Quaternion input_Quat)
	{
		Quaternion quat_rH = new Quaternion(input_Quat.x, input_Quat.y * -1.0f, input_Quat.z * -1.0f, input_Quat.w);

		return quat_rH;
	}

	protected override void Setup_Child()
    {
		
    }
    protected override void Feed_Child()
    {
		if(b_connect)
        {
			//Debug.Log("connected");
			
			// String Builder 를 이용해서, 값을 넣는다.
			sb_record = new StringBuilder();
			sb_record.Append(Frame);

			for (int p = 0; p < _actor.Bones.Length; p++)
			{
				if (p==0)
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
    }

	protected override void Read_Child()
	{
		if (b_connect && _helloRequester.bool_SendComplete == true && _helloRequester.bool_RecvComplete == true)
		{

			string message = _helloRequester.DataPostProcessing();//
			io_class.ImportConnectionData(message, _actor_target, start_root_frame,io_class.scale);
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

	[CustomEditor(typeof(doRetargeting))]
    public class doRetargeting_Editor : playEnvMotion_Editor
    {

        public doRetargeting Target_R;
        private bool ShowA;

        public new void Awake()
        {
            base.Awake();
			Target_R = (doRetargeting)target;
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
				Target_R.srcTxtFileName = EditorGUILayout.TextField("src MWTxt", Target_R.srcTxtFileName);
				if (Utility.GUIButton("write src MWTxt", UltiDraw.DarkGrey, UltiDraw.White))
				{
					genMBSTxtFile(Target_R.srcTxtFileName, Target._actor);
				}
			}

			Utility.ResetGUIColor();
			Utility.SetGUIColor(UltiDraw.LightGrey);
			using (new EditorGUILayout.VerticalScope("Box"))
			{
				Target_R.SetTargetCharacter((Actor)EditorGUILayout.ObjectField("Target Character", Target_R._actor_target, typeof(Actor), true));

				Target_R.tarTxtFileName = EditorGUILayout.TextField("tar MWTxt", Target_R.tarTxtFileName);
				if (Utility.GUIButton("write tar MWTxt", UltiDraw.DarkGrey, UltiDraw.White))
				{
					genMBSTxtFile(Target_R.tarTxtFileName, Target_R._actor_target);
				}
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
			}

		}
		

		public void genMBSTxtFile(string i_fileName, Actor i_actor)
        {
			string foldername = base.Target.directoryPath;
			Target_R.File_record = Target_R._writer_class.CreateFile(foldername, i_fileName, false, ".txt");
			Target_R.sb_record = new StringBuilder();

			Target_R.File_record.WriteLine("HIERARCHY\n");

			//Target_R.sb_record = Target_R._writer_class.WritePosition(Target_R.sb_record, i_actor.Bones[0].Transform.position, true);

			for (int i = 0; i < i_actor.Bones.Length; i++)
			{
				Vector3 pos = i_actor.Bones[i].Transform.localPosition;
				pos.Set(-1 * pos.x, pos.y, pos.z); // to MotionWorks (righthand)

				Quaternion quat = i_actor.Bones[i].Transform.localRotation;
				quat = Target_R.SET_JOINT_QUATERNION_MW(quat);

				Target_R.File_record.WriteLine("LINK");
				Target_R.File_record.WriteLine("NAME " + i_actor.Bones[i].GetName());

				// Warning: Assume that the root bone index is zero as like getRootPose
				string Joint_Type = "JOINT ACC BALL";
				if (i == 0)
				{
					Target_R.File_record.WriteLine("REF WORLD");
					Joint_Type = "JOINT ACC FREE";
				}
				else
				{
					Target_R.File_record.WriteLine("PARENT " + i_actor.Bones[i].GetParent().GetName());
					Target_R.File_record.WriteLine("REF LOCAL");
				}

				Target_R.sb_record = Target_R._writer_class.WritePosition(Target_R.sb_record, pos, false);
				Target_R.File_record.WriteLine("POS " + Target_R.sb_record.ToString());
				Target_R.sb_record.Clear();

				Target_R.sb_record = Target_R._writer_class.WriteQuat(Target_R.sb_record, quat, false);
				Target_R.File_record.WriteLine("ROT QUAT " + Target_R.sb_record.ToString());
				Target_R.sb_record.Clear();

				Target_R.File_record.WriteLine(Joint_Type);

				Target_R.File_record.WriteLine("END_LINK\n");

			}
			Target_R.File_record.WriteLine("END_HIERARCHY\n");
			
			Target_R.File_record.Close();
			Target_R.sb_record.Clear();

			
		}


		//private void InspectSkeleton(Transform transform, int indent)
		//{
		//	Actor.Bone bone = Target._actor.FindBone(transform.name);
		//	Utility.SetGUIColor(bone == null ? UltiDraw.LightGrey : UltiDraw.Mustard);
		//	using (new EditorGUILayout.HorizontalScope("Box"))
		//	{
		//		Utility.ResetGUIColor();
		//		EditorGUILayout.BeginHorizontal();
		//		EditorGUILayout.LabelField("|", GUILayout.Width(20f));
		//		EditorGUILayout.LabelField(transform.name + " " + (bone == null ? string.Empty : "(" + bone.Index.ToString() + ")" + " " + "(" + bone.GetLength() + ")"), GUILayout.Width(250f), GUILayout.Height(20f));


		//		//EditorGUILayout.LabelField("-", GUILayout.Width(20f));
		//		//EditorGUILayout.LabelField(transform.name + " " + (bone == null ? string.Empty : "(" + bone.Index.ToString() + ")" + " " + "(" + bone.GetLength() + ")"), GUILayout.Width(250f), GUILayout.Height(20f));
		//		//GUILayout.FlexibleSpace();
		//		//if (Utility.GUIButton("Bone", bone == null ? UltiDraw.White : UltiDraw.DarkGrey, bone == null ? UltiDraw.DarkGrey : UltiDraw.White))
		//		//{
		//		//	Transform[] bones = new Transform[Target.Bones.Length];
		//		//	for (int i = 0; i < bones.Length; i++)
		//		//	{
		//		//		bones[i] = Target.Bones[i].Transform;
		//		//	}
		//		//	if (bone == null)
		//		//	{
		//		//		ArrayExtensions.Add(ref bones, transform);
		//		//		Target.ExtractSkeleton(bones);
		//		//	}
		//		//	else
		//		//	{
		//		//		ArrayExtensions.Remove(ref bones, transform);
		//		//		Target.ExtractSkeleton(bones);
		//		//	}
		//		//}
		//		EditorGUILayout.EndHorizontal();
		//	}
		//	for (int i = 0; i < Target._actor.Bones.Length; i++)
		//	{
		//		InspectSkeleton(Target._actor.Bones[i].GetChild(i).Transform, indent + 1);
		//	}
		//}


	}


}
