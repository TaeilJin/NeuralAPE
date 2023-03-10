using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AI;
using System.Text;
//using Unity.Barracuda;
using System.IO;
using System;
using System.Threading;
using System.Threading.Tasks;

using NetMQ;
using NetMQ.Sockets;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

//[RequireComponent(typeof(NavMeshAgent))]

[ExecuteInEditMode]
public abstract class playEnvMotion : RealTimeAnimation
{
    public ImporterClass io_class = new ImporterClass();
    public EnvMoData[] Files = new EnvMoData[0];
    public MotionData data; 
    public int end_frame = 1000;
    public Transform visRoot;

    public int FileOrder = 0; // order 에 해당하는 File 의 index 
    public int FileIndex = 0; // order 순서 ( 0 : First ) 
    public bool Save = false;
    public bool b_play = false;
    public string directoryPath;
    public float Timestamp = 0.0f;
    public bool b_draw_total = false;
    public bool b_visualize = false;

    public GameObject Head;
    public GameObject LeftHand;
    public GameObject RightHand;

    public int findOrder(EnvMoData[] data, int order)
    {
        for (int e = 0; e < data.Length; e++)
        {
            if (order == data[e].Order)
                return e;
        }
        Debug.Log("EnvData has no Order");
        return 900;
    }

    [SerializeField] private EnvMoData Data = null;
    public void Refresh()
    {
        for (int i = 0; i < Files.Length; i++)
        {
            if (Files[i] == null)
            {
                Debug.Log("Removing missing file from editor.");
                ArrayExtensions.RemoveAt(ref Files, i);
                for (int j = 0; j < EditorSceneManager.sceneCount; j++)
                {
                    Scene scene = EditorSceneManager.GetSceneAt(j);
                    if (scene != EditorSceneManager.GetActiveScene())
                    {
                        if (!System.Array.Find(Files, x => x != null && x.GetName() == scene.name))
                        {
                            EditorSceneManager.CloseScene(scene, true);
                            break;
                        }
                    }
                }
                i--;
            }
        }
        if (Data == null && Files.Length > 0)
        {
            LoadData(Files[0]);
        }
    }
    public void LoadData(EnvMoData data)
    {
        if (Data != data)
        {
            if (Data != null)
            {
                if (Save)
                {
                    Data.Save();
                }
                Data.Unload();
            }
            if (_actor == null)
            {
                Utility.Destroy(_actor.gameObject);
            }
            Data = data;
            if (Data != null)
            {
                Data.Load();
            }
        }
    }
    [SerializeField] private Actor Actor = null;
    public void SetCharacter(Actor character)
    {
        if (_actor == null && character != null)
        {
            if (Actor != null)
            {
                Utility.Destroy(Actor.gameObject);
                Actor = null;
            }
            _actor = character;
        }
        else
        {
            _actor = character;
        }
    }
    public Actor GetActor()
    {
        if (_actor != null)
        {
            return _actor;
        }
        if (Actor == null)
        {
            Actor = Files[0].data_out.CreateActor();
            Actor.transform.SetParent(transform);
        }
        return Actor;
    }

    public UltiDraw.GUIRect Rect;
    private float[] cur_prob_value;
    public bool b_probability = false;

    public Matrix4x4 start_root_frame;
    private Vector3 desk_root_org;
    private Vector3 chair_root_org;
    //public ExperimentsUtils exp_utils;

    float[][] data_file; // Frame, goal features
    int startFrame = 1;

    public bool b_data_exist = false;
    bool b_goal_data_exist = false;
    bool b_output_data_exist = false;
    public bool b_record = false;
    int b_upper_cond = 0;

    bool b_exp = false;

    private StringBuilder sb_record;
    public StreamWriter File_record;
    private StreamWriter File_record_prob;

    protected Matrix4x4[] joint_mat_offset = new Matrix4x4[0];
       
    private StreamWriter CreateFile(string foldername, string name, bool newfile, string root_extension)
    {
        string filename = string.Empty;
        string folder = foldername;
        if (!File.Exists(folder))
        {
            Directory.CreateDirectory(folder);
            folder = folder + '/';
        }
        else
            folder = folder + "/";
        if (!File.Exists(folder + name + root_extension))
        {
            filename = folder + name + root_extension;
        }
        else
        {
            if (newfile)
            {
                int i = 1;
                while (File.Exists(folder + name + "_" + i + "_" + root_extension))
                {
                    i += 1;
                }
                filename = folder + name + "_" + i + "_" + root_extension;
            }
            else
                filename = folder + name + root_extension;
        }
        return File.CreateText(filename);
    }



    /* Event Write Function */

    //--- Current
    public void event_WriteChairRoot(GameObject chair, int fileindex)
    {
        string foldername = directoryPath;

        Matrix4x4 chair_root = chair.transform.GetWorldMatrix();

        Debug.Log("chair root of " + io_class.Files[fileindex].Object.Name);
        if (File.Exists(foldername + "/" + io_class.Files[fileindex].Object.Name))
        {

            string name = io_class.Files[fileindex].Object.Name.Replace(".bvh", "_chair");

            Debug.Log("wrtie chair root on " + name);
            File_record = CreateFile(foldername, name, false, ".txt");
            sb_record = new StringBuilder();

            sb_record = WritePosition(sb_record, chair_root.GetPosition(), true);
            sb_record = WriteQuat(sb_record, chair_root.GetRotation(), false);

            File_record.WriteLine(sb_record.ToString());

            File_record.Close();
            sb_record.Clear();
        }
    }
    public void event_WriteDeskRoot(GameObject go_desk, int fileindex)
    {
        string foldername = directoryPath;

        Matrix4x4 desk_root = go_desk.transform.GetWorldMatrix();

        Debug.Log("desk root of " + io_class.Files[fileindex].Object.Name);
        if (File.Exists(foldername + "/" + io_class.Files[fileindex].Object.Name))
        {
            string name = io_class.Files[fileindex].Object.Name.Replace(".bvh", "_desk");

            Debug.Log("wrtie desk root on " + name);

            File_record = CreateFile(foldername, name, false, ".txt");
            sb_record = new StringBuilder();

            sb_record = WritePosition(sb_record, desk_root.GetPosition(), true);
            sb_record = WriteQuat(sb_record, desk_root.GetRotation(), false);

            File_record.WriteLine(sb_record.ToString());

            File_record.Close();
            sb_record.Clear();
        }
    }

    //--- Sequence
   
    /*--- Event Load Function */

    //--- Current
    public void event_LoadRootDeskData(GameObject desk, int fileindex)
    {
        string foldername = directoryPath;

        Debug.Log("loadDirectory : " + io_class.LoadDirectory(foldername, "*.bvh"));

        string name = io_class.Files[fileindex].Object.Name.Replace(".bvh", "_desk");

        if (io_class.ImportTextRootData(foldername, name))
        {
            io_class.ImportTextRootData(foldername, name);
            desk_root_org = new Vector3(io_class.RootMat[0][0], io_class.RootMat[0][1], io_class.RootMat[0][2]);
            Quaternion quat = new Quaternion(io_class.RootMat[0][3], io_class.RootMat[0][4], io_class.RootMat[0][5], io_class.RootMat[0][6]);
            desk.transform.SetPositionAndRotation(desk_root_org, quat);

        }
        else
        {
            Debug.Log("there is no file " + name);
        }
    }
    public void event_LoadRootChairData(GameObject chair, int fileindex)
    {
        string foldername = directoryPath;

        Debug.Log("loadDirectory : " + io_class.LoadDirectory(foldername, "*.bvh"));

        string name = io_class.Files[fileindex].Object.Name.Replace(".bvh", "_chair");

        if (io_class.ImportTextRootData(foldername, name))
        {
            chair_root_org = new Vector3(io_class.RootMat[0][0], io_class.RootMat[0][1], io_class.RootMat[0][2]);
            Quaternion quat = new Quaternion(io_class.RootMat[0][3], io_class.RootMat[0][4], io_class.RootMat[0][5], io_class.RootMat[0][6]);
            chair.transform.SetPositionAndRotation(chair_root_org, quat);
        }
        else
        {
            Debug.Log("there is no file " + name);
        }
    }

    public void event_UpdateRootTR()
    {
        for (int order = 0; order < Files.Length - 1; order++)
        {
            int id_order = findOrder(Files, order);
            int id_order_next = findOrder(Files, order + 1);
            Files[id_order_next].GenerateRootTrajectory(Files[id_order].RootTr.Last<Matrix4x4>());
        }
    }
    public void event_PlayAnimation()
    {
        b_play = true;
        b_data_exist = true;
        b_output_data_exist = false;
        b_goal_data_exist = false;

        Frame = 0;// Files[FileOrder].Sequences[0].Start;
        //end_frame = Files[FileOrder].Sequences[0].End;

    }
    public void event_PauseAnimation()
    {
        b_play = false;
    }


    /* Update Function */
    public void update_pose(int index)
    {
        //Debug.Log("Frame " + index + "/" + io_class.Motion.GetLength(0));
        for (int j = 0; j < _actor.Bones.Length; j++)
            _actor.Bones[j].Transform.SetPositionAndRotation(io_class.Motion[index][j].GetPosition(), io_class.Motion[index][j].GetRotation());

    }
    public void update_pose(int index, Matrix4x4[][] _motion)
    {
        //Debug.Log("Frame " + index + "/" + _motion.GetLength(0));
        for (int j = 0; j < _actor.Bones.Length; j++)
            _actor.Bones[j].Transform.SetPositionAndRotation(_motion[index][j].GetPosition(), _motion[index][j].GetRotation());

    }
    public void update_pose(int index, Matrix4x4[][] _motion, Matrix4x4[] _root)
    {
        //Debug.Log("Frame " + index + "/" + _motion.GetLength(0));
        for (int j = 0; j < _actor.Bones.Length; j++)
        {
            Matrix4x4 jointmat = _motion[index][j].GetRelativeTransformationFrom(_root[index]);
            _actor.Bones[j].Transform.SetPositionAndRotation(jointmat.GetPosition(), jointmat.GetRotation());
        }

    }
    public void update_singlepose(Matrix4x4[] _pose)
    {
        //Debug.Log("Frame " + index + "/" + _motion.GetLength(0));
        for (int j = 0; j < _actor.Bones.Length; j++)
            _actor.Bones[j].Transform.SetPositionAndRotation(_pose[j].GetPosition(), _pose[j].GetRotation());

    }

    public CylinderMap Environment;
    private float size = 2f;
    private float resolution = 8;
    private float layers = 15;

    // root frame calculation
    private int RightShoulder = 10;
    private int LeftShoulder = 6;
    private int RightHip = 14;
    private int LeftHip = 18;
    
    public Matrix4x4 GetRootTransformation_JointTransformation(Actor actor_, float y_offset)
    {
        //vector_x
        Vector3 vec_shoulder = actor_.Bones[LeftShoulder].Transform.GetWorldMatrix().GetPosition() - actor_.Bones[RightShoulder].Transform.GetWorldMatrix().GetPosition();
        vec_shoulder = vec_shoulder.normalized;
        Vector3 vec_upleg = actor_.Bones[LeftHip].Transform.GetWorldMatrix().GetPosition() - actor_.Bones[RightHip].Transform.GetWorldMatrix().GetPosition();
        vec_upleg = vec_upleg.normalized;
        Vector3 vec_across = vec_shoulder + vec_upleg;
        vec_across = vec_across.normalized;
        //vector_forward
        Vector3 vec_forward = Vector3.Cross(-1.0f * vec_across, Vector3.up);
        //vector_x_new
        Vector3 vec_right = Vector3.Cross(-1.0f * vec_forward, Vector3.up);
        //root matrix 
        Matrix4x4 root_interaction = Matrix4x4.identity;
        Vector4 vec_x = new Vector4(vec_right.x, vec_right.y, vec_right.z, 0.0f);
        Vector4 vec_z = new Vector4(vec_forward.x, vec_forward.y, vec_forward.z, 0.0f);
        Vector4 vec_y = new Vector4(0.0f, 1.0f, 0.0f, 0.0f);
        Vector3 pos__ = actor_.Bones[0].Transform.GetWorldMatrix().GetPosition();
        Vector4 pos_h = new Vector4(pos__.x, y_offset, pos__.z, 1.0f);
        root_interaction.SetColumn(0, vec_x); root_interaction.SetColumn(1, vec_y); root_interaction.SetColumn(2, vec_z);
        root_interaction.SetColumn(3, pos_h);
        //
        return root_interaction;
    }

    public Vector3 GetRootVelocity(Matrix4x4 pre_root, Matrix4x4 current_root)
    {

        Vector3 root_vel = new Vector3();

        // rotation
        Vector3 cur_forward = pre_root.GetForward().normalized;
        Vector3 next_forward = current_root.GetForward().normalized;

        float sin = cur_forward.x * next_forward.z - next_forward.x * cur_forward.z;
        float rad_1 = Mathf.Asin(sin);

        // linear
        root_vel.z = rad_1;
        Vector3 linear_root_vel = current_root.GetPosition() - pre_root.GetPosition();
        linear_root_vel = linear_root_vel.GetRelativeDirectionTo(pre_root);
        root_vel.x = linear_root_vel.x;
        root_vel.y = linear_root_vel.z;


        return root_vel;
    }

    // you should define those function 
    protected abstract void Setup_Child();

    // update desired end-effector
    protected override void Setup()
    {
        // connection initialization
        _helloRequester = new HelloRequester();
        _helloRequester.Start(); //Thread 실행

        // io class
        io_class = new ImporterClass();
        // environment
        Environment = new CylinderMap(size, (int)resolution, (int)layers, false);

        b_play = false;
        b_record = false;
        Debug.Log("b_play " + b_play + " b_recrod " + b_record);

        Setup_Child();
    }

    protected abstract void Feed_Child();
    protected override void Feed()
    {

        // load bvh file play and write goal 
        if (b_data_exist)
        {
            // update chair 
            if (Files[FileIndex].b_same_furniture)
            {
                Files[FileIndex].Desk_mat = Files[FileIndex - 1].Desk_mat;
                Files[FileIndex].Chair_mat = Files[FileIndex - 1].Chair_mat;
            }

            if (b_play == true)
            {
                //Debug.Log(" see frame " + Frame + "/ " + Files[FileOrder].RootTr.Length);
                if (Frame == Files[FileOrder].RootTr.Length)
                {

                    FileIndex += 1;

                    if (FileIndex == Files.Length)
                    {
                        // 모든 파일이 끝났으면 첫 번째 파일을 불러온다.
                        Debug.Log("write finish: ");
                        if (b_record)
                        {
                            // goal 만드는 중이었으면, goal 을 저장한다.
                            File_record.Close();
                            b_record = false;
                        }
                        FileIndex = 0;
                        Frame = 0;
                    }
                    else
                    {
                        // fileindex 가 멀었으면, 다음 file 을 불러온다.
                        FileOrder = findOrder(Files, FileIndex);
                        Debug.Log(" file order " + FileOrder);
                        b_upper_cond = Files[FileOrder].b_uppercond;
                        if (b_record)
                            Frame = 1;
                        else
                            Frame = 0;
                    }

                }

                //Debug.Log("play Frame: " + Frame + " / " + Files[FileOrder].Sequences[0].End + " length");
                //Debug.Log(" File order " + FileOrder + " / " + Files.Length + " Files");
                // update actor pose
                update_pose(Frame, Files[FileOrder].MotionWR, Files[FileOrder].RootTr);

                if (Files[FileOrder].b_uppercond == 1)
                {
                    Vector3 position = _actor.Bones[0].Transform.position;
                    position.y += Files[FileOrder].y_offset_h;
                    _actor.Bones[0].Transform.position = position;
                }

                //
                Feed_Child(); // do Something
                
                //
                Frame++;
            }
            else
            {
                // update actor pose
                update_pose(Frame, Files[FileOrder].MotionWR, Files[FileOrder].RootTr);
                //
                Feed_Child(); // do Something
            }
        }
        else
        {
            Feed_Child();
        }
    }

    protected abstract void Read_Child();
    protected override void Read()
    {
        Read_Child();
    }

    protected override void Postprocess()
    {

    }

    protected abstract void OnRender_Child();
    protected override void OnRenderObjectDerived()
    {
        if (b_visualize)
        {
            if (b_data_exist || b_output_data_exist)
            {
                int framewidth = 30;

                UltiDraw.Begin();
                //UltiDraw.DrawWiredSphere(Files[FileOrder].StartRootMat.GetPosition(), Files[FileOrder].StartRootMat.rotation, 0.1f, UltiDraw.DarkRed, UltiDraw.Black);
                //UltiDraw.DrawTranslateGizmo(Files[FileOrder].StartRootMat.GetPosition(), Files[FileOrder].StartRootMat.rotation, 0.1f);

                //UltiDraw.DrawWiredSphere(Files[FileOrder].EndRootMat.GetPosition(), Files[FileOrder].EndRootMat.rotation, 0.1f, UltiDraw.DarkRed, UltiDraw.Black);
                //UltiDraw.DrawTranslateGizmo(Files[FileOrder].EndRootMat.GetPosition(), Files[FileOrder].EndRootMat.rotation, 0.1f);

                // start 
                UltiDraw.DrawWiredSphere(Files[FileOrder].Motion[0][5].GetPosition(), Files[FileOrder].Motion[0][5].rotation, 0.1f, UltiDraw.Red, UltiDraw.Black);
                UltiDraw.DrawWiredSphere(Files[FileOrder].Motion[0][9].GetPosition(), Files[FileOrder].Motion[0][9].rotation, 0.1f, UltiDraw.Red, UltiDraw.Black);
                UltiDraw.DrawWiredSphere(Files[FileOrder].Motion[0][13].GetPosition(), Files[FileOrder].Motion[0][13].rotation, 0.1f, UltiDraw.Red, UltiDraw.Black);
                //
                //int end_frame = 136;// (int) (Files[FileOrder].RootTr.Length / 4);
                //UltiDraw.DrawWiredSphere(Files[FileOrder].Motion[end_frame][5].GetPosition(), Files[FileOrder].Motion[end_frame][5].rotation, 0.1f, UltiDraw.Red, UltiDraw.Black);
                //UltiDraw.DrawWiredSphere(Files[FileOrder].Motion[end_frame][9].GetPosition(), Files[FileOrder].Motion[end_frame][9].rotation, 0.1f, UltiDraw.Red, UltiDraw.Black);
                //UltiDraw.DrawWiredSphere(Files[FileOrder].Motion[end_frame][13].GetPosition(), Files[FileOrder].Motion[end_frame][13].rotation, 0.1f, UltiDraw.Red, UltiDraw.Black);
                //
                end_frame = Files[FileOrder].RootTr.Length - 1;
                UltiDraw.DrawWiredSphere(Files[FileOrder].Motion[end_frame][5].GetPosition(), Files[FileOrder].Motion[end_frame][5].rotation, 0.1f, UltiDraw.Red, UltiDraw.Black);
                UltiDraw.DrawWiredSphere(Files[FileOrder].Motion[end_frame][9].GetPosition(), Files[FileOrder].Motion[end_frame][9].rotation, 0.1f, UltiDraw.Red, UltiDraw.Black);
                UltiDraw.DrawWiredSphere(Files[FileOrder].Motion[end_frame][13].GetPosition(), Files[FileOrder].Motion[end_frame][13].rotation, 0.1f, UltiDraw.Red, UltiDraw.Black);


                UltiDraw.DrawWiredSphere(Files[FileOrder].RootTr.First<Matrix4x4>().GetPosition(), Files[FileOrder].RootTr.First<Matrix4x4>().rotation, 0.1f, UltiDraw.Orange, UltiDraw.Black);
                UltiDraw.DrawTranslateGizmo(Files[FileOrder].RootTr.First<Matrix4x4>().GetPosition(), Files[FileOrder].RootTr.First<Matrix4x4>().rotation, 0.1f);

                UltiDraw.DrawWiredSphere(Files[FileOrder].RootTr.Last<Matrix4x4>().GetPosition(), Files[FileOrder].RootTr.Last<Matrix4x4>().rotation, 0.1f, UltiDraw.Green, UltiDraw.Black);
                UltiDraw.DrawTranslateGizmo(Files[FileOrder].RootTr.Last<Matrix4x4>().GetPosition(), Files[FileOrder].RootTr.Last<Matrix4x4>().rotation, 0.1f);

                UltiDraw.End();
                
                Environment.Draw(Color.green, true, false);//Draw_Dynamic_Env(Environment.Points, Environment.Occupancies)

                if (b_draw_total)
                {
                    if (Files.Length > 1)
                    {
                        UltiDraw.Begin();
                        
                        for (int order = 0; order < Files.Length - 1; order++)
                        {
                            int id_order = findOrder(Files, order);

                            for (int i = 0; i < Mathf.RoundToInt(Files[id_order].RootTr.Length / framewidth); i++)
                            {
                                UltiDraw.DrawWiredSphere(Files[id_order].RootTr[framewidth * i].GetPosition(), Files[id_order].RootTr[framewidth * i].rotation, 0.1f, UltiDraw.Orange, UltiDraw.Black);
                                UltiDraw.DrawTranslateGizmo(Files[id_order].RootTr[framewidth * i].GetPosition(), Files[id_order].RootTr[framewidth * i].rotation, 0.1f);
                            }


                            int id_order_next = findOrder(Files, order + 1);
                            //Files[id_order_next].GenerateRootTrajectory(Files[id_order].RootTr.Last<Matrix4x4>());

                            for (int i = 0; i < Mathf.RoundToInt(Files[id_order_next].RootTr.Length / framewidth); i++)
                            {
                                UltiDraw.DrawWiredSphere(Files[id_order_next].RootTr[framewidth * i].GetPosition(), Files[id_order_next].RootTr[framewidth * i].rotation, 0.1f, UltiDraw.Orange, UltiDraw.Black);
                                UltiDraw.DrawTranslateGizmo(Files[id_order_next].RootTr[framewidth * i].GetPosition(), Files[id_order_next].RootTr[framewidth * i].rotation, 0.1f);
                            }
                        }
                        UltiDraw.End();
                    }
                    else
                    {
                        UltiDraw.Begin();
                        for (int i = 0; i < Mathf.RoundToInt(Files[FileOrder].RootTr.Length / framewidth); i++)
                        {
                            UltiDraw.DrawWiredSphere(Files[FileOrder].RootTr[framewidth * i].GetPosition(), Files[FileOrder].RootTr[framewidth * i].rotation, 0.1f, UltiDraw.Orange, UltiDraw.Black);
                            UltiDraw.DrawTranslateGizmo(Files[FileOrder].RootTr[framewidth * i].GetPosition(), Files[FileOrder].RootTr[framewidth * i].rotation, 0.1f);
                            //UltiDraw.DrawTranslateGizmo(Files[FileOrder].Motion[framewidth * i][5].GetPosition(), Files[FileOrder].Motion[framewidth * i][5].rotation, 0.1f);
                        }
                        UltiDraw.End();
                    }
                }
            }

            if (b_probability)
            {
                //Debug.Log("values" + cur_prob_value);
                DrawGraph(cur_prob_value);
            }

            OnRender_Child();
        }

    }


    private void DrawGraph(float[] Values)
    {
        UltiDraw.Begin();
        Color[] colors = UltiDraw.GetRainbowColors(Values.Length);
        Vector2 pivot = Rect.GetPosition();
        float radius = 0.2f * Rect.W;
        UltiDraw.DrawGUICircle(pivot, Rect.W * 1.05f, UltiDraw.Gold);
        UltiDraw.DrawGUICircle(pivot, Rect.W, UltiDraw.White);
        Vector2[] anchors = new Vector2[Values.Length];
        for (int i = 0; i < Values.Length; i++)
        {
            float step = (float)i / (float)Values.Length;
            anchors[i] = new Vector2((Rect.W - radius / 2f) * Screen.height / Screen.width * Mathf.Cos(step * 2f * Mathf.PI), (Rect.W - radius / 2f) * Mathf.Sin(step * 2f * Mathf.PI));
        }
        Vector2[] positions = new Vector2[Values.Length];
        for (int i = 0; i < Values.Length; i++)
        {
            int _index = 0;
            positions[_index] += Values[i] * anchors[i];
            _index += 1;
        }
        for (int i = 1; i < positions.Length; i++)
        {
            UltiDraw.DrawGUILine(pivot + positions[i - 1], pivot + positions[i], 0.1f * radius, UltiDraw.Black.Transparent((float)(i + 1) / (float)positions.Length));
        }
        for (int i = 0; i < anchors.Length; i++)
        {
            UltiDraw.DrawGUILine(pivot + positions.Last(), pivot + anchors[i], 0.1f * radius, colors[i].Transparent(Values[i]));
            UltiDraw.DrawGUICircle(pivot + anchors[i], Mathf.Max(0.5f * radius, Utility.Normalise(Values[i], 0f, 1f, 0.5f, 1f) * radius), Color.Lerp(UltiDraw.Black, colors[i], Values[i]));
        }
        UltiDraw.DrawGUICircle(pivot + positions.Last(), 0.5f * radius, UltiDraw.Purple);
        UltiDraw.End();
    }


    protected override void OnGUIDerived()
    {
        //if(b_exp)
        //    if(exp_utils.ContactJoints.Length> 0)
        //    {
        //        exp_utils.ContactJoints[0].Inspector(Frame);
        //        exp_utils.ContactJoints[1].Inspector(Frame);
        //    }
    }

    public Matrix4x4[][] ArrayConcat(Matrix4x4[][] org, Matrix4x4[][] input, int start, int end)
    {
        Matrix4x4[][] result = new Matrix4x4[org.GetLength(0) + (end - start + 1)][];

        for (int id_o = 0; id_o < org.GetLength(0); id_o++)
        {
            result[id_o] = new Matrix4x4[org.GetLength(1)];
            result[id_o] = org[id_o];
        }
        for (int id = start, id_total = 0; id < (end + 1); id++, id_total++)
        {
            result[org.GetLength(0) + id_total] = new Matrix4x4[input.GetLength(1)];
            result[org.GetLength(0) + id_total] = input[id];
        }

        return result;
    }


    [CustomEditor(typeof(playEnvMotion), true)]
    public class playEnvMotion_Editor : Editor
    {
        public playEnvMotion Target;

        public void Awake()
        {
            Target = (playEnvMotion)target;

            ////connection initialization : 여기서 실행되면 시작하자마자 쓰레드가 실행된다
            //connectionSetup();
        }

        public override void OnInspectorGUI()
        {
            //base.OnInspectorGUI();
            Undo.RecordObject(Target, Target.name);
            Inspector();

            EditorGUILayout.HelpBox("Animation: " + 1000f * Target.AnimationTime + "ms", MessageType.None);

            if (GUI.changed)
            {
                EditorUtility.SetDirty(Target);
            }
        }

        public void Inspector()
        {
            Utility.SetGUIColor(UltiDraw.DarkGrey);
            using (new EditorGUILayout.VerticalScope("Box"))
            {
                Utility.ResetGUIColor();

                Utility.SetGUIColor(UltiDraw.LightGrey);
                using (new EditorGUILayout.VerticalScope("Box"))
                {
                    Utility.ResetGUIColor();

                    Target.SetCharacter((Actor)EditorGUILayout.ObjectField("Character", Target._actor, typeof(Actor), true));
                                        
                    Target.b_visualize = EditorGUILayout.Toggle("Visualize", Target.b_visualize);
                    Target.visRoot = (Transform)EditorGUILayout.ObjectField("VisRoot", Target.visRoot, typeof(Transform), true);
                    Target.io_class.scale = (float)EditorGUILayout.FloatField("scale", Target.io_class.scale);
                    EditorGUILayout.BeginHorizontal();
                    Target.directoryPath = EditorGUILayout.TextField("Folder", Target.directoryPath);
                    //--- get All BVH Files
                    if (Utility.GUIButton("Import", UltiDraw.DarkGrey, UltiDraw.White))
                    {
                        Debug.Log("push the import button ");
                        Debug.Log("loadDirectory : " + Target.io_class.LoadDirectory(Target.directoryPath, "*.bvh"));
                        if (Target.io_class.Files != null)
                            Debug.Log("DirectoryPath : " + Target.directoryPath + " files : " + Target.io_class.Files.Length);

                    }
                    EditorGUILayout.EndHorizontal();
                    // && Application.isPlaying
                    if (Target.io_class.Files != null)
                    {

                        using (new EditorGUILayout.VerticalScope("Box"))
                        {

                            //--- import BVH Data (Btn event)
                            if (Utility.GUIButton("Import all BVH", UltiDraw.DarkGrey, UltiDraw.White))
                            {
                                // for all data, inspector give a motion data inspector
                                Target.Files = new EnvMoData[Target.io_class.Files.Length];
                                // import Files
                                for (int AnimFile_Num = 0; AnimFile_Num < Target.Files.Length; AnimFile_Num++)
                                {
                                    Debug.Log("load BVH File : " + Target.io_class.ImportBVHData(Target.directoryPath, AnimFile_Num));
                                    Debug.Log("Motion Frames: " + Target.io_class.Motion.GetLength(0));

                                    // import Motion

                                    Target.Files[AnimFile_Num] = ScriptableObject.CreateInstance<EnvMoData>();
                                    Target.Files[AnimFile_Num].data_out = ScriptableObject.CreateInstance<MotionData>();
                                    Target.Files[AnimFile_Num].data_out = (MotionData)Target.io_class.data;

                                    Target.Files[AnimFile_Num].Motion = (Matrix4x4[][])Target.io_class.Motion.Clone(); // Motion
                                    Target.Files[AnimFile_Num].Index = AnimFile_Num;
                                    Target.Files[AnimFile_Num].Order = AnimFile_Num;
                                    Target.Files[AnimFile_Num].FileName = Target.io_class.Files[AnimFile_Num].Object.Name;
                                    Target.Files[AnimFile_Num].AddSequence(); // Sequence
                                    Target.Files[AnimFile_Num].GenerateRootTrajectory(0, Target.Files[AnimFile_Num].Motion.GetLength(0) - 1); // RootTr , Motion Wr
                                    Target.Files[AnimFile_Num].StartRootMat = Target.Files[AnimFile_Num].RootTr.First<Matrix4x4>();
                                    Target.Files[AnimFile_Num].EndRootMat = Target.Files[AnimFile_Num].RootTr.Last<Matrix4x4>();
                                    Target.Files[AnimFile_Num].Chair_mat = Target.Files[AnimFile_Num].RootTr.First<Matrix4x4>();
                                    Target.Files[AnimFile_Num].Desk_mat = Target.Files[AnimFile_Num].RootTr.First<Matrix4x4>();
                                    Target.Files[AnimFile_Num].GenerateRootRelative(); // seenbyChild Root realtive
                                    Target.Files[AnimFile_Num].b_uppercond = 0;
                                    Target.Files[AnimFile_Num].gui_color = UltiDraw.GetRandomColor();
                                    Debug.Log("EnvMotion Frames: " + Target.Files[AnimFile_Num].Motion.GetLength(0));
                                    //Target.Files[AnimFile_Num].Environment = new CylinderMap(Target.size, (int)Target.resolution, (int)Target.layers, false); 


                                }

                                Target.b_data_exist = true;
                                Target.b_goal_data_exist = false;

                            }
                            // -- (Btn event)

                            // -- TODO import EnvMotionData 

                            // -- EnvMotionData Inspector



                            if (Target.b_data_exist || Target.b_goal_data_exist || Target.b_output_data_exist)
                            {
                                if (Utility.GUIButton("Create Actor", UltiDraw.DarkGrey, UltiDraw.White))
                                {
                                    Target.Files[0].data_out.CreateActor();
                                }

                                //-- generate motion or generate new root trajectory ( in playmode )
                                // --- Save Chair, Desk root Mat 
                                for (int e = 0; e < Target.Files.Length; e++)
                                    Target.Files[e].inspector(Target);


                                //--Each Motion Play
                                EditorGUILayout.BeginHorizontal();
                                GUILayout.FlexibleSpace();  // 고정된 여백을 넣습니다.
                                EditorGUILayout.LabelField("Each Clip Play", GUILayout.Width(100f));
                                EditorGUILayout.LabelField("Select Order", GUILayout.Width(100f));
                                Target.FileIndex = EditorGUILayout.IntField(Target.FileIndex, GUILayout.Width(40f));
                                GUILayout.FlexibleSpace();
                                EditorGUILayout.EndHorizontal();

                                Target.FileOrder = Target.findOrder(Target.Files, Target.FileIndex);

                                if (Utility.GUIButton("update total Root Trajectory", UltiDraw.DarkGrey, UltiDraw.White))
                                {
                                    Target.event_UpdateRootTR();
                                }
                                if (Utility.GUIButton("play animation", UltiDraw.DarkGrey, UltiDraw.White))
                                {
                                    Target.event_PlayAnimation();
                                }
                                if (Utility.GUIButton("pause animation", UltiDraw.DarkGrey, UltiDraw.White))
                                {
                                    Target.event_PauseAnimation();
                                }

                                
                                //
                                Target.b_draw_total = EditorGUILayout.Toggle("Visualize Total Root Trajectory", Target.b_draw_total);

                                if (Target.b_play != true && Target.b_data_exist == true)
                                    Target.Frame = EditorGUILayout.IntSlider(Target.Frame, 1, Target.Files[Target.FileOrder].RootTr.Length - 1);
                                if (Target.b_play != true && Target.b_output_data_exist == true)
                                    Target.Frame = EditorGUILayout.IntSlider(Target.Frame, 1, Target.io_class.Motion.Length - 1);
                                

                            }
                        }

                    }


                    //Target.io_class.Refresh();
                }
            }
        }
    }

}
