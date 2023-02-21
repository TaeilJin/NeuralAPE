using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.Text;
using System.IO;

public class ImporterClass
{
    public BVHFile[] Files = null;// new BVHFile[0];
	public string Filter = string.Empty;
	public BVHFile[] Instances = new BVHFile[0];
	public Matrix4x4[][] Motion = new Matrix4x4[0][];
    public MotionData data;

    public float scale = 100.0f;
    public float[][] Prob = new float[0][];

    public float[][] Goals = new float[0][];
    public float[][] RootMat = new float[0][];
    public float[][] Markers = new float[0][];

    public string Destination = "";
	public bool LoadDirectory(string Source, string type)
	{
        if (!string.IsNullOrEmpty(Source))
        {
            if (Directory.Exists(Source))
            {
                DirectoryInfo info = new DirectoryInfo(Source);
                FileInfo[] items = info.GetFiles(type);
                Files = new BVHFile[items.Length];
                for (int i = 0; i < items.Length; i++)
                {
                    Files[i] = new BVHFile();
                    Files[i].Object = items[i];
                    Files[i].Import = true;
                }
            }
            else
            {
                //Files = new BVHFile[0];
                Files = null;
                return false;
            }
        }
        else
        {
            //Files = new BVHFile[0];
            Files = null;
            return false;
        }
        return ApplyFilter();
	}

	public bool ApplyFilter()
	{
		if (Filter == string.Empty)
		{
			Instances = Files;
		}
		else
		{
			List<BVHFile> instances = new List<BVHFile>();
			for (int i = 0; i < Files.Length; i++)
			{
				if (Files[i].Object.Name.ToLowerInvariant().Contains(Filter.ToLowerInvariant()))
				{
					instances.Add(Files[i]);
				}
			}
			Instances = instances.ToArray();
		}

        return true;
	}

    public bool ImportTextRootData(string DirectoryPath, string filename)
    {
        if (!Directory.Exists(DirectoryPath))
        {
            Debug.Log("Folder " + "'" + DirectoryPath + "'" + " is not valid.");
            return false;

        }
        else
        {
            string folder = DirectoryPath + "/" + filename + ".txt";
            if (File.Exists(folder))
            {
                string[] Output_pose; // nFrames, String RawData ( 69 + 30 + 2640 + 3 + 1 )
                Output_pose = FileUtility.ReadAllLines(folder);

                if (Output_pose.Length != 0)
                {

                    RootMat = new float[Output_pose.Length][];
                    for (int g = 0; g < Output_pose.Length; g++)
                    {
                        RootMat[g] = FileUtility.ReadArray(Output_pose[g]);
                    }

                    return true;
                }
                else
                {
                    Debug.Log("Error!: there is no Data in text file");
                    return false;
                }
            }
            else
            {
                Debug.Log("there is no file " + folder);
                return false;
            }

        }
    }
    public bool ImportTextGoalData(string DirectoryPath, string filename)
    {
        if(!Directory.Exists(DirectoryPath))
        {
            Debug.Log("Folder " + "'" + DirectoryPath + "'" + " is not valid.");
            return false;

            
        }
        else
        {
            string folder = DirectoryPath + "/" + filename + ".txt";
            if (File.Exists(folder))
            {
                string[] Output_pose; // nFrames, String RawData ( 69 + 30 + 2640 + 3 + 1 )
                Output_pose = FileUtility.ReadAllLines(folder);

                if (Output_pose.Length != 0)
                {

                    Goals = new float[Output_pose.Length][];
                    for (int g = 0; g < Output_pose.Length; g++)
                    {
                        Goals[g] = FileUtility.ReadArray(Output_pose[g]);
                    }

                    return true;
                }
                else
                {
                    Debug.Log("Error!: there is no Data in text file");
                    return false;
                }
            }
            else
            {
                Debug.Log("there is no file " + folder);
                return false;
            }

        }
    }

    public bool ImportBVHData(string DirectoryPath,int fileindex)
    {
        string destination = DirectoryPath;
        if (!Directory.Exists(destination))
        {
            Debug.Log("Folder " + "'" + destination + "'" + " is not valid.");
            return false;
        }
        else
        {
            bool b_true = false;
            if (Files[fileindex].Import)
            {
                string fileName = Files[fileindex].Object.Name.Replace(".bvh", "");
                if (!Directory.Exists(destination + "/" + fileName))
                {
                    data = ScriptableObject.CreateInstance<MotionData>();
                    string[] lines = System.IO.File.ReadAllLines(Files[fileindex].Object.FullName);
                    char[] whitespace = new char[] { ' ' };
                    int index = 0;

                    //Create Source Data
                    List<Vector3> offsets = new List<Vector3>();
                    List<int[]> channels = new List<int[]>();
                    List<float[]> motions = new List<float[]>();
                    data.Source = new MotionData.Hierarchy();
                    string name = string.Empty;
                    string parent = string.Empty;
                    Vector3 offset = Vector3.zero;
                    int[] channel = null;
                    for (index = 0; index < lines.Length; index++)
                    {
                        if (lines[index] == "MOTION")
                        {
                            break;
                        }
                        string[] entries = lines[index].Split(whitespace);
                        for (int entry = 0; entry < entries.Length; entry++)
                        {
                            if (entries[entry].Contains("ROOT"))
                            {
                                parent = "None";
                                name = entries[entry + 1];
                                break;
                            }
                            else if (entries[entry].Contains("JOINT"))
                            {
                                parent = name;
                                name = entries[entry + 1];
                                break;
                            }
                            else if (entries[entry].Contains("End"))
                            {
                                parent = name;
                                name = name + entries[entry + 1];
                                string[] subEntries = lines[index + 2].Split(whitespace);
                                for (int subEntry = 0; subEntry < subEntries.Length; subEntry++)
                                {
                                    if (subEntries[subEntry].Contains("OFFSET"))
                                    {
                                        offset.x = FileUtility.ReadFloat(subEntries[subEntry + 1]);
                                        offset.y = FileUtility.ReadFloat(subEntries[subEntry + 2]);
                                        offset.z = FileUtility.ReadFloat(subEntries[subEntry + 3]);
                                        break;
                                    }
                                }
                                data.Source.AddBone(name, parent);
                                offsets.Add(offset);
                                channels.Add(new int[0]);
                                index += 2;
                                break;
                            }
                            else if (entries[entry].Contains("OFFSET"))
                            {
                                offset.x = FileUtility.ReadFloat(entries[entry + 1]);
                                offset.y = FileUtility.ReadFloat(entries[entry + 2]);
                                offset.z = FileUtility.ReadFloat(entries[entry + 3]);
                                break;
                            }
                            else if (entries[entry].Contains("CHANNELS"))
                            {
                                channel = new int[FileUtility.ReadInt(entries[entry + 1])];
                                for (int i = 0; i < channel.Length; i++)
                                {
                                    if (entries[entry + 2 + i] == "Xposition")
                                    {
                                        channel[i] = 1;
                                    }
                                    else if (entries[entry + 2 + i] == "Yposition")
                                    {
                                        channel[i] = 2;
                                    }
                                    else if (entries[entry + 2 + i] == "Zposition")
                                    {
                                        channel[i] = 3;
                                    }
                                    else if (entries[entry + 2 + i] == "Xrotation")
                                    {
                                        channel[i] = 4;
                                    }
                                    else if (entries[entry + 2 + i] == "Yrotation")
                                    {
                                        channel[i] = 5;
                                    }
                                    else if (entries[entry + 2 + i] == "Zrotation")
                                    {
                                        channel[i] = 6;
                                    }
                                }
                                data.Source.AddBone(name, parent);
                                offsets.Add(offset);
                                channels.Add(channel);
                                break;
                            }
                            else if (entries[entry].Contains("}"))
                            {
                                name = parent;
                                parent = name == "None" ? "None" : data.Source.FindBone(name).Parent;
                                break;
                            }
                        }
                    }

                    //Set Frames
                    index += 1;
                    while (lines[index].Length == 0)
                    {
                        index += 1;
                    }
                    ArrayExtensions.Resize(ref data.Frames, FileUtility.ReadInt(lines[index].Substring(8)));

                    //Set Framerate
                    index += 1;
                    data.Framerate = Mathf.RoundToInt(1f / FileUtility.ReadFloat(lines[index].Substring(12)));

                    //Compute Frames
                    index += 1;
                    for (int i = index; i < lines.Length; i++)
                    {
                        motions.Add(FileUtility.ReadArray(lines[i]));
                    }
                    for (int k = 0; k < data.GetTotalFrames(); k++)
                    {
                        data.Frames[k] = new Frame(data, k + 1, (float)k / data.Framerate);
                        int idx = 0;
                        for (int i = 0; i < data.Source.Bones.Length; i++)
                        {
                            MotionData.Hierarchy.Bone info = data.Source.Bones[i];
                            Vector3 position = Vector3.zero;
                            Quaternion rotation = Quaternion.identity;

                            for (int j = 0; j < channels[i].Length; j++)
                            {
                                if (channels[i][j] == 1)
                                {
                                    position.x = motions[k][idx]; idx += 1;
                                }
                                if (channels[i][j] == 2)
                                {
                                    position.y = motions[k][idx]; idx += 1;
                                }
                                if (channels[i][j] == 3)
                                {
                                    position.z = motions[k][idx]; idx += 1;
                                }
                                if (channels[i][j] == 4)
                                {
                                    rotation *= Quaternion.AngleAxis(motions[k][idx], Vector3.right); idx += 1;
                                }
                                if (channels[i][j] == 5)
                                {
                                    rotation *= Quaternion.AngleAxis(motions[k][idx], Vector3.up); idx += 1;
                                }
                                if (channels[i][j] == 6)
                                {
                                    rotation *= Quaternion.AngleAxis(motions[k][idx], Vector3.forward); idx += 1;
                                }
                            }

                            position = (position == Vector3.zero ? offsets[i] : position) / scale; //unit scale
                            Matrix4x4 local = Matrix4x4.TRS(position, rotation, Vector3.one);
                            local = local.GetMirror(Axis.XPositive);

                            data.Frames[k].World[i] = info.Parent == "None" ? local : data.Frames[k].World[data.Source.FindBone(info.Parent).Index] * local;
                        }

                      
                    }

                    if (data.GetTotalFrames() == 1)
                    {
                        Frame reference = data.Frames.First();
                        ArrayExtensions.Resize(ref data.Frames, Mathf.RoundToInt(data.Framerate));
                        for (int k = 0; k < data.GetTotalFrames(); k++)
                        {
                            data.Frames[k] = new Frame(data, k + 1, (float)k / data.Framerate);
                            data.Frames[k].World = (Matrix4x4[])reference.World.Clone();
                        }
                    }

                    //Detect Symmetry
                    data.DetectSymmetry();
                
                    //
                    Debug.Log(data.Framerate + " FPS ");
                    if (data.Framerate == 60)
                    {
                        int width = Mathf.RoundToInt(data.Framerate / 26);
                        int totalframes = Mathf.RoundToInt(data.GetTotalFrames() / width);
                        Motion = new Matrix4x4[totalframes][];
                        for (int k = 0; k < totalframes; k++)
                        {
                            Motion[k] = new Matrix4x4[data.Frames[width * k].World.Length];
                            Motion[k] = data.Frames[width * k].World;

                            //for (int i = 0; i < data.Source.Bones.Length; i++)
                            //    Motion[k,i] = data.Frames[k].World[i];
                        }
                    }
                    else
                    {
                        Motion = new Matrix4x4[data.GetTotalFrames()][];
                        for (int k = 0; k < data.GetTotalFrames(); k++)
                        {
                            Motion[k] = new Matrix4x4[data.Frames[k].World.Length];
                            Motion[k] = data.Frames[k].World;

                            //for (int i = 0; i < data.Source.Bones.Length; i++)
                            //    Motion[k,i] = data.Frames[k].World[i];
                        }
                    }
                    b_true =  true;
                }
                else
                {
                    Debug.Log("File with name " + fileName + " already exists.");
                    b_true = false;
                }

            }

            return b_true;
        }

        

    }
    public bool ImportConnectionDataProb(string message, Actor _actor, Matrix4x4 root_mat, out float[] prob)
    {
        var splittedStrings = message.Split(' ');
        //Debug.Log(" get " + splittedStrings[0] + " " + splittedStrings.Length);
        int total_length = (22 * 4 + 3) + 1 + (10);
        prob = new float[10];
        if (splittedStrings[0] == "Hello" && splittedStrings.Length == total_length)
        {
            var x = float.Parse(splittedStrings[1 + 0]);
            var y = float.Parse(splittedStrings[1 + 1]);
            var z = float.Parse(splittedStrings[1 + 2]);
            Vector3 pos = new Vector3(x, y, z);

            //_actor.Bones[0].Transform.position = pos / 100.0f;
            pos = pos / 100.0f;
            _actor.Bones[0].Transform.position = pos;

            for (int j = 0; j < _actor.Bones.Length; j++)
            {
                var quat_x = float.Parse(splittedStrings[(1 + 4 * j + 3) + 0]);
                var quat_y = float.Parse(splittedStrings[(1 + 4 * j + 3) + 1]);
                var quat_z = float.Parse(splittedStrings[(1 + 4 * j + 3) + 2]);
                var quat_w = float.Parse(splittedStrings[(1 + 4 * j + 3) + 3]);

                Quaternion quat_lH = new Quaternion(quat_x, quat_y, quat_z, quat_w);

                _actor.Bones[j].Transform.localRotation = quat_lH;

            }

            Matrix4x4 bone_hip = _actor.Bones[0].Transform.GetWorldMatrix();
            bone_hip = bone_hip.GetRelativeTransformationFrom(root_mat);
            _actor.Bones[0].Transform.SetPositionAndRotation(bone_hip.GetPosition(), bone_hip.GetRotation());

            
            for (int k=0; k <10; k++)
                prob[k] = float.Parse(splittedStrings[(1 + 4 * 22 + 3) + k]);
            

            return true;

        }
        else
        {
            Debug.Log("check pose data");
            return false;
        }


    }
    public bool ImportConnectionData(string message, Actor _actor, Matrix4x4 root_mat)
    {
        var splittedStrings = message.Split(' ');
        //Debug.Log(" get " + splittedStrings[0] + " " + splittedStrings.Length);
        int total_length = (22 * 4 + 3) + 1;
        if (splittedStrings[0] == "Hello" && splittedStrings.Length == total_length)
        {
            var x = float.Parse(splittedStrings[1 + 0]);
            var y = float.Parse(splittedStrings[1 + 1]);
            var z = float.Parse(splittedStrings[1 + 2]);
            Vector3 pos = new Vector3(x, y, z);

            //_actor.Bones[0].Transform.position = pos / 100.0f;
            pos = pos / 100.0f;
            _actor.Bones[0].Transform.position = pos;

            for (int j = 0; j < _actor.Bones.Length; j++)
            {
                var quat_x = float.Parse(splittedStrings[(1 + 4 * j + 3) + 0]);
                var quat_y = float.Parse(splittedStrings[(1 + 4 * j + 3) + 1]);
                var quat_z = float.Parse(splittedStrings[(1 + 4 * j + 3) + 2]);
                var quat_w = float.Parse(splittedStrings[(1 + 4 * j + 3) + 3]);

                Quaternion quat_lH = new Quaternion(quat_x, quat_y, quat_z, quat_w);

                _actor.Bones[j].Transform.localRotation = quat_lH;

            }

            Matrix4x4 bone_hip = _actor.Bones[0].Transform.GetWorldMatrix();
            bone_hip = bone_hip.GetRelativeTransformationFrom(root_mat);
            _actor.Bones[0].Transform.SetPositionAndRotation(bone_hip.GetPosition(), bone_hip.GetRotation());


            return true;

        }
        else
        {
            Debug.Log("check pose data");
            return false;
        }

        
    }
    public bool ImportConnectionData(string message, Actor _actor, Matrix4x4 root_mat, float scale)
    {
        var splittedStrings = message.Split(' ');
        //Debug.Log(" get " + splittedStrings[0] + " " + splittedStrings.Length);
        //Debug.Log(" scale " + scale);
        int total_length = (_actor.Bones.Length * 4 + 3) + 1;
        if (splittedStrings[0] == "Hello" && splittedStrings.Length == total_length)
        {
            float x; float y; float z;
            float.TryParse(splittedStrings[1 + 0],out x);
            float.TryParse(splittedStrings[1 + 1],out y);
            float.TryParse(splittedStrings[1 + 2],out z);
            Vector3 pos = new Vector3(x, y, z);

            //_actor.Bones[0].Transform.position = pos / 100.0f;
            pos = pos / scale;
            _actor.Bones[0].Transform.position = pos;
            //Debug.Log(" pos " + pos + "actor" + _actor.Bones[0].Transform.position);

            for (int j = 0; j < _actor.Bones.Length; j++)
            {
                float w;
                float.TryParse(splittedStrings[(1 + 4 * j + 3) + 0],out x);
                float.TryParse(splittedStrings[(1 + 4 * j + 3) + 1],out y);
                float.TryParse(splittedStrings[(1 + 4 * j + 3) + 2],out z);
                float.TryParse(splittedStrings[(1 + 4 * j + 3) + 3],out w);

                Quaternion quat_lH = new Quaternion(x, y, z, w);

                _actor.Bones[j].Transform.localRotation = quat_lH;

            }

            return true;

        }
        else
        {
            Debug.Log("check pose data");
            return false;
        }


    }

    public bool ImportOutputMotionData(string DirectoryPath, int fileindex, int bones)
    {
        string destination = DirectoryPath;
        if (!Directory.Exists(destination))
        {
            Debug.Log("Folder " + "'" + destination + "'" + " is not valid.");
            return false;
        }
        else
        {
            bool b_true;
            Debug.Log("load File : " + Files[fileindex].Object.Name);
            if (Files[fileindex].Import)
            {
                string[] Output; // nFrames, String RawData ( 69 + 30 + 2640 + 3 + 1 )
                Output = FileUtility.ReadAllLines(Files[fileindex].Object.FullName);

                Motion = new Matrix4x4[Output.Length][];
                
                for (int k = 0; k < Output.Length; k++)
                {
                    float[] pose_data = FileUtility.ReadArray(Output[k]);
                    Motion[k] = new Matrix4x4[bones];
                    for (int i = 0; i < bones; i++)
                    {
                        Matrix4x4 world_mat = new Matrix4x4();

                        Vector3 position = new Vector3(pose_data[7 * i + 0], pose_data[7 * i + 1], pose_data[7 * i + 2]);
                        Quaternion quat = new Quaternion(pose_data[7 * i + 3], pose_data[7 * i + 4], pose_data[7 * i + 5], pose_data[7 * i + 6]);
                        
                        world_mat.SetTRS(position, quat, Vector3.one);

                        Motion[k][i] = world_mat;
                    }
                    
                }

                // probabilitiy
                string fileName = Files[fileindex].Object.FullName.Replace(".csv", "_prob.csv");
                Debug.Log(fileName);
                ImportTextFloatArrayData(fileName, 10, out Prob);

                b_true = true;
            }
            else
            {
                b_true = false;
            }
               
            return b_true;
        }
    }

    public bool ImportTextMarkerData(string DirectoryPath, string filename)
    {
        if (!Directory.Exists(DirectoryPath))
        {
            Debug.Log("Folder " + "'" + DirectoryPath + "'" + " is not valid.");
            return false;


        }
        else
        {
            string folder = DirectoryPath + "/" + filename + ".txt";
            if (File.Exists(folder))
            {
                string[] Output_pose; // nFrames, String RawData ( 69 + 30 + 2640 + 3 + 1 )
                Output_pose = FileUtility.ReadAllLines(folder);

                if (Output_pose.Length != 0)
                {

                    Markers = new float[Output_pose.Length][];
                    for (int g = 0; g < Output_pose.Length; g++)
                    {
                        Markers[g] = FileUtility.ReadArray(Output_pose[g]);
                    }

                    return true;
                }
                else
                {
                    Debug.Log("Error!: there is no Data in text file");
                    return false;
                }
            }
            else
            {
                Debug.Log("there is no file " + folder);
                return false;
            }

        }
    }

    public bool ImportTextFloatArrayData(string FullName, int rows, out float[][] _outarray)
    {

        string[] Output; // nFrames, String RawData ( 69 + 30 + 2640 + 3 + 1 )
        Output = FileUtility.ReadAllLines(FullName);

        _outarray = new float[Output.Length][];

        for (int k = 0; k < Output.Length; k++)
        {
            float[] pose_data = FileUtility.ReadArray(Output[k]);
            _outarray[k] = new float[rows];
            for (int i = 0; i < rows; i++)
            {
                _outarray[k][i] = pose_data[i];
            }

        }

        return true;

    }


    public bool ImportTextData(string DirectoryPath, string filename)
    {
        if (!Directory.Exists(DirectoryPath))
        {
            Debug.Log("Folder " + "'" + DirectoryPath + "'" + " is not valid.");
            return false;


        }
        else
        {
            string folder = DirectoryPath + "/" + filename + ".txt";
            if (File.Exists(folder))
            {
                string[] Output_pose; // nFrames, String RawData ( 69 + 30 + 2640 + 3 + 1 )
                Output_pose = FileUtility.ReadAllLines(folder);

                if (Output_pose.Length != 0)
                {

                    Markers = new float[Output_pose.Length][];
                    for (int g = 0; g < Output_pose.Length; g++)
                    {
                        Markers[g] = FileUtility.ReadArray(Output_pose[g]);
                    }

                    return true;
                }
                else
                {
                    Debug.Log("Error!: there is no Data in text file");
                    return false;
                }
            }
            else
            {
                Debug.Log("there is no file " + folder);
                return false;
            }

        }
    }
    private StreamWriter File_record;
    private StringBuilder sb_record;
    private StreamWriter CreateFile(string foldername, string name)
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
        if (!File.Exists(folder + name + ".txt"))
        {
            filename = folder + name;
        }
        else
        {
            //int i = 1;
            //while (File.Exists(folder + name + " (" + i + ").txt"))
            //{
            //    i += 1;
            //}
            //filename = folder + name + " (" + i + ")";
            filename = folder + name;
        }
        return File.CreateText(filename + ".txt");
    }
    private StringBuilder WriteFloat(StringBuilder sb_, float x, bool first)
    {
        if (first)
        {
            sb_.Append(x);
        }
        else
        {
            sb_.Append(" ");
            sb_.Append(x);
        }
        return sb_;
    }
    private StringBuilder WritePosition(StringBuilder sb_, Vector3 position, bool first)
    {
        sb_ = WriteFloat(sb_, position.x, first);
        sb_ = WriteFloat(sb_, position.y, false);
        sb_ = WriteFloat(sb_, position.z, false);

        return sb_;
    }
    private StringBuilder WriteQuat(StringBuilder sb_, Quaternion quat, bool first)
    {
        sb_ = WriteFloat(sb_, quat.x, first);
        sb_ = WriteFloat(sb_, quat.y, false);
        sb_ = WriteFloat(sb_, quat.z, false);
        sb_ = WriteFloat(sb_, quat.w, false);

        return sb_;
    }
    public bool WriteMatData(string DirectoryPath, string filename, Matrix4x4 root_mat)
    {
        
        if (Directory.Exists(DirectoryPath))
        {
            Debug.Log("wrtie matrix on " + filename);

            File_record = CreateFile(DirectoryPath, filename);
            sb_record = new StringBuilder();

            sb_record = WritePosition(sb_record, root_mat.GetPosition(), true);
            sb_record = WriteQuat(sb_record, root_mat.GetRotation(), false);

            File_record.WriteLine(sb_record.ToString());

            File_record.Close();
            sb_record.Clear();

            return true;
        }
        else
        {
            return false;
        }
    }
    public void Refresh()
    {
        Files = new BVHFile[0];
        Filter = string.Empty;
        Instances = new BVHFile[0];
        Motion = new Matrix4x4[0][];
        Goals = new float[0][];
        RootMat = new float[0][];
    }   
    //public void ImportBVHData(string DirectoryPath)
    //{
    //	string destination = DirectoryPath + "/" + Destination;
    //	if (!AssetDatabase.IsValidFolder(destination))
    //	{
    //		Debug.Log("Folder " + "'" + destination + "'" + " is not valid.");
    //	}else
    //	{
    //		for (int f = 0; f < Files.Length; f++)
    //		{
    //			if (Files[f].Import)
    //			{
    //				string fileName = Files[f].Object.Name.Replace(".bvh", "");
    //				if (!Directory.Exists(destination + "/" + fileName))
    //				{
    //					MotionData data = ScriptableObject.CreateInstance<MotionData>();
    //					string[] lines = System.IO.File.ReadAllLines(Files[f].Object.FullName);
    //					char[] whitespace = new char[] { ' ' };
    //					int index = 0;

    //					//Create Source Data
    //					List<Vector3> offsets = new List<Vector3>();
    //					List<int[]> channels = new List<int[]>();
    //					List<float[]> motions = new List<float[]>();
    //					data.Source = new MotionData.Hierarchy();
    //					string name = string.Empty;
    //					string parent = string.Empty;
    //					Vector3 offset = Vector3.zero;
    //					int[] channel = null;
    //					for (index = 0; index < lines.Length; index++)
    //					{
    //						if (lines[index] == "MOTION")
    //						{
    //							break;
    //						}
    //						string[] entries = lines[index].Split(whitespace);
    //						for (int entry = 0; entry < entries.Length; entry++)
    //						{
    //							if (entries[entry].Contains("ROOT"))
    //							{
    //								parent = "None";
    //								name = entries[entry + 1];
    //								break;
    //							}
    //							else if (entries[entry].Contains("JOINT"))
    //							{
    //								parent = name;
    //								name = entries[entry + 1];
    //								break;
    //							}
    //							else if (entries[entry].Contains("End"))
    //							{
    //								parent = name;
    //								name = name + entries[entry + 1];
    //								string[] subEntries = lines[index + 2].Split(whitespace);
    //								for (int subEntry = 0; subEntry < subEntries.Length; subEntry++)
    //								{
    //									if (subEntries[subEntry].Contains("OFFSET"))
    //									{
    //										offset.x = FileUtility.ReadFloat(subEntries[subEntry + 1]);
    //										offset.y = FileUtility.ReadFloat(subEntries[subEntry + 2]);
    //										offset.z = FileUtility.ReadFloat(subEntries[subEntry + 3]);
    //										break;
    //									}
    //								}
    //								data.Source.AddBone(name, parent);
    //								offsets.Add(offset);
    //								channels.Add(new int[0]);
    //								index += 2;
    //								break;
    //							}
    //							else if (entries[entry].Contains("OFFSET"))
    //							{
    //								offset.x = FileUtility.ReadFloat(entries[entry + 1]);
    //								offset.y = FileUtility.ReadFloat(entries[entry + 2]);
    //								offset.z = FileUtility.ReadFloat(entries[entry + 3]);
    //								break;
    //							}
    //							else if (entries[entry].Contains("CHANNELS"))
    //							{
    //								channel = new int[FileUtility.ReadInt(entries[entry + 1])];
    //								for (int i = 0; i < channel.Length; i++)
    //								{
    //									if (entries[entry + 2 + i] == "Xposition")
    //									{
    //										channel[i] = 1;
    //									}
    //									else if (entries[entry + 2 + i] == "Yposition")
    //									{
    //										channel[i] = 2;
    //									}
    //									else if (entries[entry + 2 + i] == "Zposition")
    //									{
    //										channel[i] = 3;
    //									}
    //									else if (entries[entry + 2 + i] == "Xrotation")
    //									{
    //										channel[i] = 4;
    //									}
    //									else if (entries[entry + 2 + i] == "Yrotation")
    //									{
    //										channel[i] = 5;
    //									}
    //									else if (entries[entry + 2 + i] == "Zrotation")
    //									{
    //										channel[i] = 6;
    //									}
    //								}
    //								data.Source.AddBone(name, parent);
    //								offsets.Add(offset);
    //								channels.Add(channel);
    //								break;
    //							}
    //							else if (entries[entry].Contains("}"))
    //							{
    //								name = parent;
    //								parent = name == "None" ? "None" : data.Source.FindBone(name).Parent;
    //								break;
    //							}
    //						}
    //					}

    //					//Set Frames
    //					index += 1;
    //					while (lines[index].Length == 0)
    //					{
    //						index += 1;
    //					}
    //					ArrayExtensions.Resize(ref data.Frames, FileUtility.ReadInt(lines[index].Substring(8)));

    //					//Set Framerate
    //					index += 1;
    //					data.Framerate = Mathf.RoundToInt(1f / FileUtility.ReadFloat(lines[index].Substring(12)));

    //					//Compute Frames
    //					index += 1;
    //					for (int i = index; i < lines.Length; i++)
    //					{
    //						motions.Add(FileUtility.ReadArray(lines[i]));
    //					}
    //					for (int k = 0; k < data.GetTotalFrames(); k++)
    //					{
    //						data.Frames[k] = new Frame(data, k + 1, (float)k / data.Framerate);
    //						int idx = 0;
    //						for (int i = 0; i < data.Source.Bones.Length; i++)
    //						{
    //							MotionData.Hierarchy.Bone info = data.Source.Bones[i];
    //							Vector3 position = Vector3.zero;
    //							Quaternion rotation = Quaternion.identity;

    //							for (int j = 0; j < channels[i].Length; j++)
    //							{
    //								if (channels[i][j] == 1)
    //								{
    //									position.x = motions[k][idx]; idx += 1;
    //								}
    //								if (channels[i][j] == 2)
    //								{
    //									position.y = motions[k][idx]; idx += 1;
    //								}
    //								if (channels[i][j] == 3)
    //								{
    //									position.z = motions[k][idx]; idx += 1;
    //								}
    //								if (channels[i][j] == 4)
    //								{
    //									rotation *= Quaternion.AngleAxis(motions[k][idx], Vector3.right); idx += 1;
    //								}
    //								if (channels[i][j] == 5)
    //								{
    //									rotation *= Quaternion.AngleAxis(motions[k][idx], Vector3.up); idx += 1;
    //								}
    //								if (channels[i][j] == 6)
    //								{
    //									rotation *= Quaternion.AngleAxis(motions[k][idx], Vector3.forward); idx += 1;
    //								}
    //							}

    //							position = (position == Vector3.zero ? offsets[i] : position) / 100f; //unit scale
    //							Matrix4x4 local = Matrix4x4.TRS(position, rotation, Vector3.one);
    //							local = local.GetMirror(Axis.XPositive);

    //							data.Frames[k].World[i] = info.Parent == "None" ? local : data.Frames[k].World[data.Source.FindBone(info.Parent).Index] * local;
    //						}

    //						/*
    //						for(int i=0; i<data.Source.Bones.Length; i++) {
    //							data.Frames[k].Local[i] *= Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(data.Corrections[i]), Vector3.one);
    //							data.Frames[k].World[i] *= Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(data.Corrections[i]), Vector3.one);
    //						}
    //						*/
    //					}

    //					if (data.GetTotalFrames() == 1)
    //					{
    //						Frame reference = data.Frames.First();
    //						ArrayExtensions.Resize(ref data.Frames, Mathf.RoundToInt(data.Framerate));
    //						for (int k = 0; k < data.GetTotalFrames(); k++)
    //						{
    //							data.Frames[k] = new Frame(data, k + 1, (float)k / data.Framerate);
    //							data.Frames[k].World = (Matrix4x4[])reference.World.Clone();
    //						}
    //					}

    //					//Detect Symmetry
    //					data.DetectSymmetry();

    //					//Add Scene
    //					data.CreateScene();
    //					data.AddSequence();

    //					//Save
    //					EditorUtility.SetDirty(data);

    //					Motion = new Matrix4x4[data.GetTotalFrames()][];
    //					for (int k=0; k < data.GetTotalFrames(); k++)
    //						for (int i=0; i < data.Source.Bones.Length; i++)
    //							Motion[k][i] = data.Frames[k].World[i];
    //				}
    //				else
    //				{
    //					Debug.Log("File with name " + fileName + " already exists.");
    //				}

    //			}
    //		}

    //	}


    //}



    public class BVHFile
	{
		public FileInfo Object;
		public bool Import;
	}

}