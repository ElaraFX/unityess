/**************************************************************************
 * Copyright (C) 2016 Rendease Co., Ltd.
 * All rights reserved.
 *
 * This program is commercial software: you must not redistribute it 
 * and/or modify it without written permission from Rendease Co., Ltd.
 *
 * This program is distributed in the hope that it will be useful, 
 * but WITHOUT ANY WARRANTY; without even the implied warranty of 
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the 
 * End User License Agreement for more details.
 *
 * You should have received a copy of the End User License Agreement along 
 * with this program.  If not, see <http://www.rendease.com/licensing/>
 *************************************************************************/

using System.Collections;
using System.IO;
using System.Collections.Generic;
using System;
using System.Text;
using UnityEngine;
using UnityEngine.Assertions;

public class EssWriter {
    StringBuilder essDataString = new StringBuilder(); /**< ess字符流数据 */

    public bool writeToFile = false;
    string path = ""; /**< ess写入文件路径 */
    string essFileName = "";
    
    public StreamWriter CreateFile(string path, string name)
    {
        StreamWriter sw;
        FileInfo t = new FileInfo(path + "/" + name);
        if (t.Exists)
        {
            t.Delete();
        }
        sw = t.CreateText();
        return sw;
    }

    public void Initialize(string path, string filename)
    {
        writeToFile = true;
        this.path = path;
        essFileName = filename;
    }

    public void Initialize()
    {
        writeToFile = false;
    }

    public void Close()
    {
        if(writeToFile)
        {
            StreamWriter sw = CreateFile(path, essFileName);
            sw.WriteLine(essDataString);
            sw.Close();
            sw.Dispose();
        }
    }

	public void BeginNode(string type, string name)
    {        
        essDataString.Append(String.Format("\nnode \"{0}\" \"{1}\"", type, name));
    }

	public void LinkParam(string input, string shader, string output)
    {
        essDataString.Append(String.Format("\n\tparam_link \"{0}\" \"{1}\" \"{2}\"", input, shader, output));
    }

	public void AddScaler(string name, float value)
    {
        essDataString.Append(String.Format("\n\tscalar \"{0}\" {1}", name, value));
    }

	public void AddInt(string name, int value)
    {
        essDataString.Append(String.Format("\n\tint \"{0}\" {1}", name, value));
    }

	public void AddVector4(string name, Vector4 value)
    {
        essDataString.Append(String.Format("\n\tvector4 \"{0}\" {1} {2} {3} {4}", name, value.x, value.y, value.z, value.w));
    }

	public void AddVector3(string name, Vector3 value)
    {
        essDataString.Append(String.Format("\n\tvector \"{0}\" {1} {2} {3}", name, value.x, value.y, value.z));
    }

	public void AddToken(string name, string value)
    {
        essDataString.Append(String.Format("\n\ttoken \"{0}\" \"{1}\"", name, value));
    }

	public void AddColor(string name, Vector4 value)
    {
        essDataString.Append(String.Format("\n\tcolor \"{0}\" {1} {2} {3}", name, value.x * value.w, value.y * value.w, value.z * value.w));
    }

	public void AddColor(string name, Vector3 value)
    {
        essDataString.Append(String.Format("\n\tcolor \"{0}\" {1} {2} {3}", name, value.x, value.y, value.z));
    }

	public void AddBool(string name, bool value)
    {
        essDataString.Append(String.Format("\n\tbool \"{0}\" {1}", name, (value ? "on" : "off")));
    }

	public void AddRef(string name, string refVal)
    {
        essDataString.Append(String.Format("\n\tref \"{0}\" \"{1}\"", name, refVal));
    }

	public void AddRefGroup(string grouptype, List<string> refelements)
    {
        essDataString.Append(String.Format("\n\tref[] \"{0}\" 1", grouptype));
        foreach(string var in refelements)
        {
            essDataString.Append(String.Format("\n\t\t\"{0}\"", var));
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
			    //matStr.Append(matrix[row,col].ToString() << " ";
                matStr = matStr + val + " ";
		    }		
	    }
        essDataString.Append(String.Format("\n\tmatrix \"{0}\" {1}", name, matStr));
    }

	public void AddEnum(string name, string value)
    {
        essDataString.Append(String.Format("\n\tenum \"{0}\" \"{1}\"", name, value));
    }

	public void AddRenderCommand(string inst_group_name, string cam_name, string option_name)
    {
        essDataString.Append(String.Format("\nrender \"{0}\" \"{1}\" \"{2}\"", inst_group_name, cam_name, option_name));
    }

	public void AddDeclare()
    {
        essDataString.Append(String.Format("\n\tdeclare"));
    }

	public void AddDeclare(string type, string name, string storage_class)
    {
        essDataString.Append(String.Format("\n\tdeclare {0} \"{1}\" {2}", type, name, storage_class));
    }

    public void AddIndexArray(string name, int[] indexs, bool faceVarying)
    {
        essDataString.Append(String.Format("\n\tindex[] \"{0}\" 1", name));

        Assert.IsTrue(faceVarying == false); //暂时不加faceVarying

        string lineIndexDataStr = "";
        for (int i = 0; i < indexs.Length; ++i)
		{            
            lineIndexDataStr = lineIndexDataStr + indexs[i] + " ";
		}
        essDataString.Append(String.Format("\n\t\t{0}", lineIndexDataStr));

    }

    public void AddPointArray(string name, Vector3[] vectorArray)
    {
        essDataString.Append(String.Format("\n\tpoint[] \"{0}\" 1", name));
        for(int i = 0; i < vectorArray.Length; ++i)
        {
            essDataString.Append(String.Format("\n\t\t {0} {1} {2}", vectorArray[i].x, vectorArray[i].y, vectorArray[i].z));
        }
    }

    public void AddCustomString(string str)
    {
        essDataString.Append(String.Format("\n{0}", str));
    }

    public void EndNode()
    {
        essDataString.Append("\nend");
    }

    public void BeginNameSpace(string spaceName)
    {
        essDataString.Append(String.Format("\nnamespace \"{0}\"", spaceName));
    }

    public void EndNameSpace()
    {
        essDataString.Append("\nend namespace");
    }

    public void addParseEss(string essName)
    {
        essDataString.Append(String.Format("\n\tparse2 \"{0}\" on", essName));
    }

    public string getEssDataString()
    {
        return essDataString.ToString();
    }
}
