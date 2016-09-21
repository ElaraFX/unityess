using UnityEngine;
using System.Collections;
using System.IO;
using System.Collections.Generic;
using System;
using System.Text;
using UnityEngine.Assertions;

public class EssWriter : MonoBehaviour {
    //ess data
    StreamWriter sw; //write to file
    string essString; //write to string

    public bool writeToString = true;

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}
    
    public void CreateFile(string path, string name)
    {
        //文件流信息
        Debug.Log("path = " + path);
        FileInfo t = new FileInfo(path + "//" + name);
        if (!t.Exists)
        {
            //如果此文件不存在则创建
            sw = t.CreateText();
        }
        else
        {
            //如果此文件存在删除内容
            t.Delete();
            sw = t.CreateText();
        }
    }

    public void Initialize(string path, string filename)
    {
        writeToString = false;
        CreateFile(path, filename);
    }

    public void Initialize()
    {
        writeToString = true;
    }

    public void Close()
    {
        if(!writeToString)
        {
            sw.Close();
            sw.Dispose();
        }
    }

	public void BeginNode(string type, string name)
    {        
        sw.WriteLine("node \"{0}\" \"{1}\"", type, name);
    }

	public void LinkParam(string input, string shader, string output)
    {
        sw.WriteLine("\tparam_link \"{0}\" \"{1}\" \"{2}\"", input, shader, output);
    }

	public void AddScaler(string name, float value)
    {
        sw.WriteLine("\tscalar \"{0}\" {1}", name, value);
    }

	public void AddInt(string name, int value)
    {
        sw.WriteLine("\tint \"{0}\" {1}", name, value);
    }

	public void AddVector4(string name, Vector4 value)
    {
        sw.WriteLine("\tvector4 \"{0}\" {1} {2} {3} {4}", name, value.x, value.y, value.z, value.w);
    }

	public void AddVector3(string name, Vector3 value)
    {
        sw.WriteLine("\tvector \"{0}\" {1} {2} {3}", name, value.x, value.y, value.z);
    }

	public void AddToken(string name, string value)
    {
        sw.WriteLine("\ttoken \"{0}\" {1}", name, value);
    }

	public void AddColor(string name, Vector4 value)
    {
        sw.WriteLine("\tcolor \"{0}\" {1} {2} {3}", name, value.x * value.w, value.y * value.w, value.z * value.w);
    }

	public void AddColor(string name, Vector3 value)
    {
        sw.WriteLine("\tcolor \"{0}\" {1} {2} {3}", name, value.x, value.y, value.z);
    }

	public void AddBool(string name, bool value)
    {
        sw.WriteLine("\tbool \"{0}\" {1}", name, (value ? "on" : "off"));
    }

	public void AddRef(string name, string refVal)
    {
        sw.WriteLine("\tref \"{0}\" \"{1}\"", name, refVal);
    }

	public void AddRefGroup(string grouptype, List<string> refelements)
    {
        sw.WriteLine("\tref[] \"{0}\" 1", grouptype);
        foreach(string var in refelements)
        {
            sw.WriteLine("\t\t\"{0}\"", var);
        }
    }

	public void AddMatrix(string name, Matrix4x4 matrix)
    {
        string matStr = "";
        for (int row = 0; row < 4; ++row)
	    {
		    for (int col = 0; col < 4; ++col)
		    {   
                float val = matrix[row, col];
			    //matStr += matrix[row,col].ToString() << " ";
                matStr = matStr + val + " ";
		    }		
	    }
        sw.WriteLine("\tmatrix \"{0}\" {1}", name, matStr);
    }

	public void AddEnum(string name, string value)
    {
        sw.WriteLine("\tenum \"{0}\" \"{1}\"", name, value);
    }

	public void AddRenderCommand(string inst_group_name, string cam_name, string option_name)
    {
        sw.WriteLine("render \"{0}\" \"{1}\" \"{2}\"", inst_group_name, cam_name, option_name);
    }

	public void AddDeclare()
    {
        sw.WriteLine("\tdeclare");
    }

	public void AddDeclare(string type, string name, string storage_class)
    {
        sw.WriteLine("\tdeclare {0} \"{1}\" {2}", type, name, storage_class);
    }

    public void AddIndexArray(string name, int[] indexs, bool faceVarying)
    {
        sw.WriteLine("\tindex[] \"{0}\" 1", name);

        Assert.IsTrue(faceVarying == false); //暂时不加faceVarying

        string lineIndexDataStr = "";
        for (int i = 0; i < indexs.Length; ++i)
		{            
            lineIndexDataStr = lineIndexDataStr + indexs[i] + " ";
		}
        sw.WriteLine("\t\t{0}", lineIndexDataStr);

    }

    public void AddPointArray(string name, Vector3[] vectorArray)
    {
        sw.WriteLine("\tpoint[] \"{0}\" 1", name);

        for(int i = 0; i < vectorArray.Length; ++i)
        {
            sw.WriteLine("\t\t {0} {1} {2}", vectorArray[i].x, vectorArray[i].y, vectorArray[i].z);
        }
    }

    public void AddCustomString(string str)
    {
        sw.WriteLine("{0}", str);
    }

    public void EndNode()
    {
        sw.WriteLine("end");
    }
}
