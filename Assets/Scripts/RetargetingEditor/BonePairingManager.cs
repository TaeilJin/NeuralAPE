using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class BonePairingManager : MonoBehaviour
{
    [Header("Drag and Drop Before Playing")]
    public GameObject Source;
    public GameObject Target;  

    // UI
    [Header("UI")]
    public Text Error_Txt;
    public Text S_Name_Txt;
    public GameObject S_Content;
    public Text T_Name_Txt;
    public GameObject T_Content;
    public GameObject P_Content;
    public GameObject NodeObj;
    public Button Build_Btn;
    public Button Pair_Btn;
    public Button Unpair_Btn;
    public Button Upload_Btn;
    public Button Save_Btn;

    private Actor S_Actor = null;
    private Actor T_Actor = null;

    public static BoneNodeController CurSCtrl = null;
    public static BoneNodeController CurTCtrl = null;
    public static HashSet<string> SelPName = new HashSet<string>();
    private Dictionary<string, BoneNodeController> PairedNodeList = new Dictionary<string, BoneNodeController>();

    private WriterClass w_class;

    // Start is called before the first frame update
    void Start()
    {
        w_class = new WriterClass();
        //w_class.CreateFile()
    }

    // Update is called once per frame
    void Update()
    {
        if (NullErrorCheck()) return;
    }

    // Button ON OFF & Error MSG
    private bool NullErrorCheck()
    {
        bool check = false;
        if (Source == null || Target == null)
        {
            Error_Txt.text = "Reference the source and target correctly. Seems to be empty some part.";
            Build_Btn.interactable = false;
            Pair_Btn.interactable = false;
            Unpair_Btn.interactable = false;
            Upload_Btn.interactable = false;
            Save_Btn.interactable = false;
            check = true;
        }
        else
        {
            Error_Txt.text = "";
            S_Name_Txt.text = Source.name;
            T_Name_Txt.text = Target.name;
            
            Build_Btn.interactable = true;
            Upload_Btn.interactable = true;

            if (CurSCtrl != null && CurTCtrl != null) Pair_Btn.interactable = true;
            else Pair_Btn.interactable = false;

            if (SelPName.Count > 0) Unpair_Btn.interactable = true;
            else Unpair_Btn.interactable = false;

            if (PairedNodeList.Count > 0) Save_Btn.interactable = true;
            else Save_Btn.interactable = false;
        }
        return check;
    }


    // --------------------UI Trigger Function--------------------
    
    public void BuildBoneListOnClick()
    {
        if (S_Actor == null)
        {
            S_Actor = Source.GetComponent<Actor>();
        }

        if (S_Actor != null)
        {
            for (int i = 0; i < S_Actor.Bones.Length; i++)
            {
                GameObject tempObj = Instantiate(NodeObj);
                tempObj.transform.SetParent(S_Content.transform, false);
                BoneNodeController tempNodeCtrl = tempObj.GetComponent<BoneNodeController>();
                tempNodeCtrl.BoneType = BoneType.SOURCE;
                tempNodeCtrl.Index = i;
                tempNodeCtrl.BoneName = S_Actor.Bones[i].GetName();
                tempNodeCtrl.GetComponentInChildren<Text>().text = tempNodeCtrl.BoneName;
            }
        }

        if (T_Actor == null)
        {
            T_Actor = Target.GetComponent<Actor>();
        }

        if (T_Actor != null)
        {
            for (int i = 0; i < T_Actor.Bones.Length; i++)
            {
                GameObject tempObj = Instantiate(NodeObj);
                tempObj.transform.SetParent(T_Content.transform, false);
                BoneNodeController tempNodeCtrl = tempObj.GetComponent<BoneNodeController>();
                tempNodeCtrl.BoneType = BoneType.TARGET;
                tempNodeCtrl.Index = i;
                tempNodeCtrl.BoneName = T_Actor.Bones[i].GetName();
                tempNodeCtrl.GetComponentInChildren<Text>().text = tempNodeCtrl.BoneName;
            }
        }
    }

    public void PairingOnClick()
    {
        // Pairing : 2개였던 BoneNodeController를 1개로 합침
        GameObject tempObj = Instantiate(NodeObj);
        tempObj.transform.SetParent(P_Content.transform, false);
        BoneNodeController tempNodeCtrl = tempObj.GetComponent<BoneNodeController>();
        tempNodeCtrl.BoneType = BoneType.PAIRED;
        tempNodeCtrl.Index = CurSCtrl.Index;
        tempNodeCtrl.BoneName = CurSCtrl.BoneName;
        tempNodeCtrl.Index_T = CurTCtrl.Index;
        tempNodeCtrl.BoneName_T = CurTCtrl.BoneName;
        tempNodeCtrl.GetComponentInChildren<Text>().text = tempNodeCtrl.BoneName + " + " + tempNodeCtrl.BoneName_T;
        PairedNodeList.Add(CurSCtrl.BoneName, tempNodeCtrl);
        Destroy(CurSCtrl.gameObject);
        Destroy(CurTCtrl.gameObject);
        CurSCtrl = null;
        CurTCtrl = null;
    }

    public void UnpairingOnClick()
    {
        // Unpairing: pairing과 반대로, 1개였던 BoneNodeController를 2개로 쪼갬
        foreach (string name in SelPName)
        {
            Debug.Log(name);
            if (!PairedNodeList.ContainsKey(name)) continue;

            // Source
            GameObject tempObj = Instantiate(NodeObj);
            tempObj.transform.SetParent(S_Content.transform, false);
            BoneNodeController tempNodeCtrl = tempObj.GetComponent<BoneNodeController>();
            tempNodeCtrl.BoneType = BoneType.SOURCE;
            tempNodeCtrl.Index = PairedNodeList[name].Index;
            tempNodeCtrl.BoneName = PairedNodeList[name].BoneName;
            tempNodeCtrl.GetComponentInChildren<Text>().text = tempNodeCtrl.BoneName;

            // Target
            GameObject tempObj_T = Instantiate(NodeObj);
            tempObj_T.transform.SetParent(T_Content.transform, false);
            BoneNodeController tempNodeCtrl_T = tempObj_T.GetComponent<BoneNodeController>();
            tempNodeCtrl_T.BoneType = BoneType.TARGET;
            tempNodeCtrl_T.Index = PairedNodeList[name].Index_T;
            tempNodeCtrl_T.BoneName = PairedNodeList[name].BoneName_T;
            tempNodeCtrl_T.GetComponentInChildren<Text>().text = tempNodeCtrl_T.BoneName;

            Destroy(PairedNodeList[name].gameObject);
            PairedNodeList[name] = null;
            PairedNodeList.Remove(name);
        }
    }

    public void SaveCsvOnClick()
    {
        string writeFilepath = EditorUtility.SaveFilePanel("Overwrite with csv", "", Source.name + "+" + Target.name + ".csv", "csv");
        Debug.Log("Filepath : " + writeFilepath);
        if (writeFilepath.Length != 0)
        {           
            // Write
            string[,] output = new string[PairedNodeList.Count, 4];            
            int i = 0;
            foreach (KeyValuePair<string, BoneNodeController> unit in PairedNodeList)
            {
                output[i, 0] = PairedNodeList[unit.Key].Index.ToString();
                output[i, 1] = PairedNodeList[unit.Key].BoneName;
                output[i, 2] = PairedNodeList[unit.Key].Index_T.ToString();
                output[i, 3] = PairedNodeList[unit.Key].BoneName_T;
                i++;
            }
            int length = output.GetLength(0);
            string delimiter = " ";
            StringBuilder stringBuilder = new StringBuilder();
            for (int index = 0; index < length; index++)
            {
                stringBuilder.AppendLine(string.Join(delimiter, output[index, 0], 
                    output[index, 1], output[index, 2], output[index, 3]));
            }
            StreamWriter outStream = File.CreateText(writeFilepath);
            outStream.Write(stringBuilder);
            outStream.Close();
        }
    }

    public void UploadCsvOnClick()
    {
        string readFilepath = EditorUtility.OpenFilePanel("Overwrite with csv", "", "csv");
        if (!File.Exists(readFilepath))
        {
            Error_Txt.text = "File Path(" + readFilepath + ") Not Exists.";
            return;
        }

        string value = "";
        StreamReader reader = new StreamReader(readFilepath);
        value = reader.ReadToEnd();
        reader.Close();

        Debug.Log(value);
        
        // Parsing how?
        //string[] lines = value.Split("\n"[0]);
        //string[] header = SplitCsvLine(lines[0]);
        //string[] values = SplitCsvLine(lines[1]);

        ////Debug.Log("readDNA.Count : " + readDNA.Count);
        //for (int i = 0; i < header.Length; i++)
        //{
        //    //Debug.Log(header[i] + " : " + values[i]);
        //    if (readDNA.ContainsKey(header[i])) readDNA[header[i]].Set(float.Parse(values[i]));
        //}
    }

    //public string[] SplitCsvLine(string line)
    //{
    //    return (from Match m in System.Text.RegularExpressions.Regex.Matches(line,
    //    @"(((?<x>(?=[,\r\n]+))|""(?<x>([^""]|"""")+)""|(?<x>[^,\r\n]+)),?)",
    //    RegexOptions.ExplicitCapture)
    //            select m.Groups[1].Value).ToArray();
    //}
}
