using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.Text;
using System.IO;

public class WriterClass
{
    private StreamWriter File_record;
    private StringBuilder sb_record; // string builder 
    public StreamWriter CreateFile(string foldername, string name, bool newfile, string root_extension)
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
    public StringBuilder WriteFloat(StringBuilder sb_, float x, bool first)
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
    public StringBuilder WritePosition(StringBuilder sb_, Vector3 position, bool first)
    {
        sb_ = WriteFloat(sb_, position.x, first);
        sb_ = WriteFloat(sb_, position.y, false);
        sb_ = WriteFloat(sb_, position.z, false);

        return sb_;
    }
    public StringBuilder WriteQuat(StringBuilder sb_, Quaternion quat, bool first)
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

            File_record = CreateFile(DirectoryPath, filename,true,".txt");
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
    

}