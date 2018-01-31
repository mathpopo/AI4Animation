﻿using UnityEngine;

public static class UnityGL {

	private static Material SceneMaterial;
	private static Material GUIMaterial;

	private static bool Drawing = false;

	private static bool Initialised = false;

	private static PROGRAM Program = PROGRAM.NONE;

	private enum PROGRAM {NONE, LINES, TRIANGLES, TRIANGLE_STRIP, QUADS};

	private static float GUIOffset = 0.001f;

	private static Vector3 ViewPosition = Vector3.zero;
	private static Quaternion ViewRotation = Quaternion.identity;

	private static int CircleResolution = 16;
	private static Vector3[] CirclePoints;
	private static int SphereResolution = 16;
	private static Vector3[] SpherePoints;

	private static Material CurrentMaterial;

	static void Initialise() {
		if(Initialised) {
			return;
		}

		Resources.UnloadUnusedAssets();

		Shader colorShader = Shader.Find("Hidden/Internal-Colored");

		SceneMaterial = new Material(colorShader);
		SceneMaterial.hideFlags = HideFlags.HideAndDontSave;
		SceneMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
		SceneMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
		SceneMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Back);
		SceneMaterial.SetInt("_ZWrite", 0);

		GUIMaterial = new Material(colorShader);
		GUIMaterial.hideFlags = HideFlags.HideAndDontSave;
		GUIMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
		GUIMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
		GUIMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
		GUIMaterial.SetInt("_ZWrite", 0);

		//Circle
		CirclePoints = new Vector3[CircleResolution];
		for(int i=0; i<CircleResolution; i++) {
			float angle = 2f * Mathf.PI * (float)i / ((float)CircleResolution-1f);
			CirclePoints[i] = CircleVertex(angle);
		}

		//Sphere
		SpherePoints = new Vector3[4 * SphereResolution * SphereResolution];
		float startU=0;
		float startV=0;
		float endU=Mathf.PI*2;
		float endV=Mathf.PI;
		float stepU=(endU-startU)/SphereResolution;
		float stepV=(endV-startV)/SphereResolution;
		int index = 0;
		for(int i=0; i<SphereResolution; i++) {
			for(int j=0; j<SphereResolution; j++) {
				float u=i*stepU+startU;
				float v=j*stepV+startV;
				float un=(i+1==SphereResolution) ? endU : (i+1)*stepU+startU;
				float vn=(j+1==SphereResolution) ? endV : (j+1)*stepV+startV;
				SpherePoints[index+0] = SphereVertex(u, v);
				SpherePoints[index+1] = SphereVertex(u, vn);
				SpherePoints[index+2] = SphereVertex(un, v);
				SpherePoints[index+3] = SphereVertex(un, vn);
				index += 4;
			}
		}

		Initialised = true;
	}

	private static Vector3 CircleVertex(float angle) {
 		return new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
	}

	private static Vector3 SphereVertex(float u, float v) {
		return new Vector3(Mathf.Cos(u)*Mathf.Sin(v), Mathf.Cos(v), Mathf.Sin(u)*Mathf.Sin(v));
	}

	private static void SetProgram(PROGRAM program, Material material) {
		if(Program != program || CurrentMaterial != material) {
			GL.End();
			Program = program;
			CurrentMaterial = material;
			if(Program != PROGRAM.NONE) {
				CurrentMaterial.SetPass(0);
				switch(Program) {
					case PROGRAM.LINES:
					GL.Begin(GL.LINES);
					break;
					case PROGRAM.TRIANGLES:
					GL.Begin(GL.TRIANGLES);
					break;
					case PROGRAM.TRIANGLE_STRIP:
					GL.Begin(GL.TRIANGLE_STRIP);
					break;
					case PROGRAM.QUADS:
					GL.Begin(GL.QUADS);
					break;
				}
			}
		}
	}

	public static void Start() {
		if(Drawing) {
			Debug.Log("Drawing has not been finished yet.");
		} else {
			Initialise();
			ViewPosition = GetCamera().transform.position;
			ViewRotation = GetCamera().transform.rotation;
			Drawing = true;
		}
	}

	public static void Finish() {
		if(Drawing) {
			SetProgram(PROGRAM.NONE, null);
			Drawing = false;
		} else {
			ViewPosition = Vector3.zero;
			ViewRotation = Quaternion.identity;
			Debug.Log("Drawing has not been started yet.");
		}
	}

	//------------------------------------------------------------------------------------------
	//SCENE DRAWING FUNCTIONS
	//------------------------------------------------------------------------------------------
	public static void DrawLine(Vector3 start, Vector3 end, Color color) {
		if(Return()) {return;}
		SetProgram(PROGRAM.LINES, SceneMaterial);
		GL.Color(color);
		GL.Vertex(start);
		GL.Vertex(end);
	}

	public static void DrawLine(Vector3 start, Vector3 end, Color startColor, Color endColor) {
		if(Return()) {return;}
		SetProgram(PROGRAM.LINES, SceneMaterial);
		GL.Color(startColor);
		GL.Vertex(start);
		GL.Color(endColor);
		GL.Vertex(end);
	}

    public static void DrawLine(Vector3 start, Vector3 end, float width, Color color) {
		if(Return()) {return;}
		SetProgram(PROGRAM.QUADS, SceneMaterial);
		Vector3 dir = (end-start).normalized;
		Vector3 orthoStart = width/2f * (Quaternion.AngleAxis(90f, (start - ViewPosition)) * dir);
		Vector3 orthoEnd = width/2f * (Quaternion.AngleAxis(90f, (end - ViewPosition)) * dir);
		
		GL.Color(color);
		GL.Vertex(end+orthoEnd);
		GL.Vertex(end-orthoEnd);
		GL.Vertex(start-orthoStart);
		GL.Vertex(start+orthoStart);
    }

    public static void DrawLine(Vector3 start, Vector3 end, float startWidth, float endWidth, Color color) {
		if(Return()) {return;}
		SetProgram(PROGRAM.QUADS, SceneMaterial);
		Vector3 dir = (end-start).normalized;
		Vector3 orthoStart = startWidth/2f * (Quaternion.AngleAxis(90f, (start - ViewPosition)) * dir);
		Vector3 orthoEnd = endWidth/2f * (Quaternion.AngleAxis(90f, (end - ViewPosition)) * dir);

		GL.Color(color);
		GL.Vertex(end+orthoEnd);
		GL.Vertex(end-orthoEnd);
		GL.Vertex(start-orthoStart);
		GL.Vertex(start+orthoStart);
    }

    public static void DrawLine(Vector3 start, Vector3 end, float width, Color startColor, Color endColor) {
		if(Return()) {return;}
		SetProgram(PROGRAM.QUADS, SceneMaterial);
		Vector3 dir = (end-start).normalized;
		Vector3 orthoStart = width/2f * (Quaternion.AngleAxis(90f, (start - ViewPosition)) * dir);
		Vector3 orthoEnd = width/2f * (Quaternion.AngleAxis(90f, (end - ViewPosition)) * dir);

		GL.Color(endColor);
		GL.Vertex(end+orthoEnd);
		GL.Vertex(end-orthoEnd);
		GL.Color(startColor);
		GL.Vertex(start-orthoStart);
		GL.Vertex(start+orthoStart);
    }

    public static void DrawLine(Vector3 start, Vector3 end, float startWidth, float endWidth, Color startColor, Color endColor) {
		if(Return()) {return;}
		SetProgram(PROGRAM.QUADS, SceneMaterial);
		Vector3 dir = (end-start).normalized;
		Vector3 orthoStart = startWidth/2f * (Quaternion.AngleAxis(90f, (start - ViewPosition)) * dir);
		Vector3 orthoEnd = endWidth/2f * (Quaternion.AngleAxis(90f, (end - ViewPosition)) * dir);

		GL.Color(endColor);
		GL.Vertex(end+orthoEnd);
		GL.Vertex(end-orthoEnd);
		GL.Color(startColor);
		GL.Vertex(start-orthoStart);
		GL.Vertex(start+orthoStart);
    }


	public static void DrawTriangle(Vector3 a, Vector3 b, Vector3 c, Color color) {
		if(Return()) {return;}
		SetProgram(PROGRAM.TRIANGLES, SceneMaterial);
        GL.Color(color);
        GL.Vertex(a);
		GL.Vertex(b);
		GL.Vertex(c);
	}

	public static void DrawTriangle(Vector3 center, float radius, Color color) {
		if(Return()) {return;}
		SetProgram(PROGRAM.TRIANGLES, SceneMaterial);
        GL.Color(color);
        GL.Vertex(center + ViewRotation*(radius*new Vector3(-0.5f, 0.5f, 0f)));
		GL.Vertex(center + ViewRotation*(radius*new Vector3(0.5f, 0.5f, 0f)));
		GL.Vertex(center + ViewRotation*(radius*new Vector3(0f, -0.5f, 0f)));
	}

	public static void DrawQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Color color) {
		if(Return()) {return;}
		SetProgram(PROGRAM.QUADS, SceneMaterial);
        GL.Color(color);
        GL.Vertex(a);
		GL.Vertex(c);
		GL.Vertex(d);
		GL.Vertex(b);
	}

	public static void DrawQuad(Vector3 center, float width, float height, Color color) {
		if(Return()) {return;}
		SetProgram(PROGRAM.QUADS, SceneMaterial);
        GL.Color(color);
        GL.Vertex(center + ViewRotation*(new Vector3(-0.5f*width, -0.5f*height, 0f)));
		GL.Vertex(center + ViewRotation*(new Vector3(0.5f*width, -0.5f*height, 0f)));
		GL.Vertex(center + ViewRotation*(new Vector3(0.5f*width, 0.5f*height, 0f)));
        GL.Vertex(center + ViewRotation*(new Vector3(-0.5f*width, 0.5f*height, 0f)));
	}

	public static void DrawWireQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Color color) {
		if(Return()) {return;}
		DrawLine(a, b, color);
		DrawLine(b, d, color);
		DrawLine(d, c, color);
		DrawLine(c, a, color);
	}

	public static void DrawWireQuad(Vector3 center, float width, float height, Color color) {
		if(Return()) {return;}
		Vector3 a = center + ViewRotation*(new Vector3(-0.5f*width, -0.5f*height, 0f));
		Vector3 b = center + ViewRotation*(new Vector3(0.5f*width, -0.5f*height, 0f));
		Vector3 c = center + ViewRotation*(new Vector3(0.5f*width, 0.5f*height, 0f));
        Vector3 d = center + ViewRotation*(new Vector3(-0.5f*width, 0.5f*height, 0f));
		DrawWireQuad(d, a, c, b, color);
	}

	public static void DrawWiredQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Color quadColor, Color wireColor) {
		if(Return()) {return;}
		DrawQuad(a, b, c, d, quadColor);
		DrawWireQuad(a, b, c, d, wireColor);
	}

	public static void DrawWiredQuad(Vector3 center, float width, float height, Color quadColor, Color wireColor) {
		if(Return()) {return;}
		DrawQuad(center, width, height, quadColor);
		DrawWireQuad(center, width, height, wireColor);
	}

	public static void DrawCircle(Vector3 center, float radius, Color color) {
		if(Return()) {return;}
		SetProgram(PROGRAM.TRIANGLES, SceneMaterial);
        GL.Color(color);
		for(int i=0; i<CircleResolution-1; i++) {
			GL.Vertex(center + radius*(ViewRotation*CirclePoints[i]));
			GL.Vertex(center);
			GL.Vertex(center + radius*(ViewRotation*CirclePoints[i+1]));
		}
	}

	public static void DrawWireCircle(Vector3 center, float radius, Color color) {
		if(Return()) {return;}
		for(int i=1; i<CircleResolution; i++) {
			DrawLine(center + radius*(ViewRotation*CirclePoints[i-1]), center + radius*(ViewRotation*CirclePoints[i]), color);
		}
	}

	public static void DrawWiredCircle(Vector3 center, float radius, Color circleColor, Color wireColor) {
		if(Return()) {return;}
		DrawCircle(center, radius, circleColor);
		DrawWireCircle(center, radius, wireColor);
	}

	public static void DrawSphere(Vector3 center, float radius, Color color) {
		if(Return()) {return;}
		SetProgram(PROGRAM.QUADS, SceneMaterial);
        GL.Color(color);
		int index = 0;
		for(int i=0; i<SphereResolution; i++) {
			for(int j=0; j<SphereResolution; j++) {
				GL.Vertex(center + radius*SpherePoints[index+0]);
				GL.Vertex(center + radius*SpherePoints[index+2]);
				GL.Vertex(center + radius*SpherePoints[index+3]);
				GL.Vertex(center + radius*SpherePoints[index+1]);
				index += 4;
			}
		}
	}

	public static void DrawWireSphere(Vector3 center, float radius, Color color) {
		if(Return()) {return;}
		SetProgram(PROGRAM.LINES, SceneMaterial);
        GL.Color(color);
		int index = 0;
		for(int i=0; i<SphereResolution; i++) {
			index += 2*SphereResolution;
			Vector3 a = center + radius*SpherePoints[index+0];
			Vector3 b = center + radius*SpherePoints[index+2];
			GL.Vertex(a);
			GL.Vertex(b);
			GL.Vertex(new Vector3(a.z, a.x, a.y));
			GL.Vertex(new Vector3(b.z, b.x, b.y));
			GL.Vertex(new Vector3(a.y, a.z, a.x));
			GL.Vertex(new Vector3(b.y, b.z, b.x));
			index += 2*SphereResolution;
		}
	}

	public static void DrawWiredSphere(Vector3 center, float radius, Color sphereColor, Color wireColor) {
		if(Return()) {return;}
		DrawSphere(center, radius, sphereColor);
		DrawWireSphere(center, radius, wireColor);
	}

	public static void DrawCube(Vector3 center, Quaternion rotation, float size, Color color) {
		if(Return()) {return;}
		Vector3 A = rotation*new Vector3(-size/2f, -size/2f, -size/2f);
		Vector3 B = rotation*new Vector3(size/2f, -size/2f, -size/2f);
		Vector3 C = rotation*new Vector3(-size/2f, -size/2f, size/2f);
		Vector3 D = rotation*new Vector3(size/2f, -size/2f, size/2f);
		Vector3 p1 = center + A; Vector3 p2 = center + B;
		Vector3 p3 = center + C; Vector3 p4 = center + D;
		Vector3 p5 = center - D; Vector3 p6 = center - C;
		Vector3 p7 = center - B; Vector3 p8 = center - A;
		DrawQuad(p2, p1, p4, p3, color);
		DrawQuad(p1, p2, p5, p6, color);
		DrawQuad(p5, p6, p7, p8, color);
		DrawQuad(p4, p3, p8, p7, color);
		DrawQuad(p2, p4, p6, p8, color);
		DrawQuad(p3, p1, p7, p5, color);
	}

	public static void DrawWireCube(Vector3 center, Quaternion rotation, float size, Color color) {
		if(Return()) {return;}
		Vector3 A = rotation*new Vector3(-size/2f, -size/2f, -size/2f);
		Vector3 B = rotation*new Vector3(size/2f, -size/2f, -size/2f);
		Vector3 C = rotation*new Vector3(-size/2f, -size/2f, size/2f);
		Vector3 D = rotation*new Vector3(size/2f, -size/2f, size/2f);
		Vector3 p1 = center + A; Vector3 p2 = center + B;
		Vector3 p3 = center + C; Vector3 p4 = center + D;
		Vector3 p5 = center - D; Vector3 p6 = center - C;
		Vector3 p7 = center - B; Vector3 p8 = center - A;
		DrawLine(p1, p2, color); DrawLine(p2, p4, color);
		DrawLine(p4, p3, color); DrawLine(p3, p1, color);
		DrawLine(p5, p6, color); DrawLine(p6, p8, color);
		DrawLine(p5, p7, color); DrawLine(p7, p8, color);
		DrawLine(p1, p5, color); DrawLine(p2, p6, color);
		DrawLine(p3, p7, color); DrawLine(p4, p8, color);
	}

	public static void DrawWiredCube(Vector3 center, Quaternion rotation, float size, Color cubeColor, Color wireColor) {
		if(Return()) {return;}
		Vector3 A = rotation*new Vector3(-size/2f, -size/2f, -size/2f);
		Vector3 B = rotation*new Vector3(size/2f, -size/2f, -size/2f);
		Vector3 C = rotation*new Vector3(-size/2f, -size/2f, size/2f);
		Vector3 D = rotation*new Vector3(size/2f, -size/2f, size/2f);
		Vector3 p1 = center + A; Vector3 p2 = center + B;
		Vector3 p3 = center + C; Vector3 p4 = center + D;
		Vector3 p5 = center - D; Vector3 p6 = center - C;
		Vector3 p7 = center - B; Vector3 p8 = center - A;
		DrawQuad(p2, p1, p4, p3, cubeColor);
		DrawQuad(p1, p2, p5, p6, cubeColor);
		DrawQuad(p5, p6, p7, p8, cubeColor);
		DrawQuad(p4, p3, p8, p7, cubeColor);
		DrawQuad(p2, p4, p6, p8, cubeColor);
		DrawQuad(p3, p1, p7, p5, cubeColor);
		DrawLine(p1, p2, wireColor); DrawLine(p2, p4, wireColor);
		DrawLine(p4, p3, wireColor); DrawLine(p3, p1, wireColor);
		DrawLine(p5, p6, wireColor); DrawLine(p6, p8, wireColor);
		DrawLine(p5, p7, wireColor); DrawLine(p7, p8, wireColor);
		DrawLine(p1, p5, wireColor); DrawLine(p2, p6, wireColor);
		DrawLine(p3, p7, wireColor); DrawLine(p4, p8, wireColor);
	}

	public static void DrawCone(Vector3 center, float width, float height, Color color) {
		if(Return()) {return;}
		SetProgram(PROGRAM.TRIANGLES, SceneMaterial);
        GL.Color(color);
		for(int i=0; i<CircleResolution-1; i++) {
			Vector3 a = center + width * new Vector3(CirclePoints[i].x, CirclePoints[i].z, CirclePoints[i].y);
			Vector3 b = center + width * new Vector3(CirclePoints[i+1].x, CirclePoints[i+1].z, CirclePoints[i+1].y);
			GL.Vertex(center);
			GL.Vertex(a);
			GL.Vertex(b);

			GL.Vertex(a);
			GL.Vertex(center + new Vector3(0f, height, 0f));
			GL.Vertex(b);
		}
	}

	public static void DrawWireCone(Vector3 center, float width, float height, Color color) {
		if(Return()) {return;}
		SetProgram(PROGRAM.LINES, SceneMaterial);
        GL.Color(color);
		for(int i=0; i<CircleResolution-1; i++) {
			Vector3 a = center + width * new Vector3(CirclePoints[i].x, CirclePoints[i].z, CirclePoints[i].y);
			Vector3 b = center + width * new Vector3(CirclePoints[i+1].x, CirclePoints[i+1].z, CirclePoints[i+1].y);
			GL.Vertex(a);
			GL.Vertex(b);

			GL.Vertex(a);
			GL.Vertex(center + new Vector3(0f, height, 0f));
		}
	}

	public static void DrawWiredCone(Vector3 center, float width, float height, Color coneColor, Color wireColor) {
		if(Return()) {return;}
		DrawCone(center, width, height, coneColor);
		DrawWireCone(center, width, height, wireColor);
	}

	public static void DrawArrow(Vector3 start, Vector3 end, float tipPivot, float shaftWidth, float tipWidth, Color color) {
		if(Return()) {return;}
		if(tipPivot < 0f || tipPivot > 1f) {
			Debug.Log("The tip pivot must be specified between 0 and 1.");
			tipPivot = Mathf.Clamp(tipPivot, 0f, 1f);
		}
		Vector3 pivot = start + tipPivot * (end-start);
		DrawLine(start, pivot, shaftWidth, color);
		DrawLine(pivot, end, tipWidth, 0f, color);
	}

	public static void DrawArrow(Vector3 start, Vector3 end, float tipPivot, float shaftWidth, float tipWidth, Color shaftColor, Color tipColor) {
		if(Return()) {return;}
		if(tipPivot < 0f || tipPivot > 1f) {
			Debug.Log("The tip pivot must be specified between 0 and 1.");
			tipPivot = Mathf.Clamp(tipPivot, 0f, 1f);
		}
		Vector3 pivot = start + tipPivot * (end-start);
		DrawLine(start, pivot, shaftWidth, shaftColor);
		DrawLine(pivot, end, tipWidth, 0f, tipColor);
	}

	public static void DrawSpline(Vector3[] controlPoints, int sampling, Color color) {
		if(Return()) {return;}
		Vector3[] points = GetCatmullRomSpline(controlPoints, sampling);
		for(int i=1; i<points.Length; i++) {
			DrawLine(points[i-1], points[i], color);
		}
	}

	public static void DrawSpline(Vector3[] controlPoints, int sampling, float width, Color color) {
		if(Return()) {return;}
		Vector3[] points = GetCatmullRomSpline(controlPoints, sampling);
		for(int i=1; i<points.Length; i++) {
			DrawLine(points[i-1], points[i], width, color);
		}
	}

	public static void DrawMesh(Mesh mesh, Vector3 position, Quaternion rotation, Vector3 scale, Material material) {
		if(Return()) {return;}
		SetProgram(PROGRAM.NONE, material);
		material.SetPass(0);
		Graphics.DrawMeshNow(mesh, Matrix4x4.TRS(position, rotation, scale));
	}

	//------------------------------------------------------------------------------------------
	//GUI DRAWING FUNCTIONS
	//------------------------------------------------------------------------------------------
	public static void DrawGUILine(float xStart, float yStart, float xEnd, float yEnd, Color color) {
		if(Return()) {return;}
		xStart *= Screen.width;
		yStart *= Screen.height;
		xEnd *= Screen.width;
		yEnd *= Screen.height;
		Camera camera = GetCamera();
		Vector3 start = camera.ScreenToWorldPoint(new Vector3(xStart, yStart, camera.nearClipPlane + GUIOffset));
		Vector3 end = camera.ScreenToWorldPoint(new Vector3(xEnd, yEnd, camera.nearClipPlane + GUIOffset));
		SetProgram(PROGRAM.LINES, GUIMaterial);
		GL.Color(color);
		GL.Vertex(start);
		GL.Vertex(end);
	}

    public static void DrawGUILine(float xStart, float yStart, float xEnd, float yEnd, float width, Color color) {
		if(Return()) {return;}
		xStart *= Screen.width;
		yStart *= Screen.height;
		xEnd *= Screen.width;
		yEnd *= Screen.height;
		width /= Screen.width;
		Camera camera = GetCamera();
		Vector3 start = camera.ScreenToWorldPoint(new Vector3(xStart, yStart, camera.nearClipPlane + GUIOffset));
		Vector3 end = camera.ScreenToWorldPoint(new Vector3(xEnd, yEnd, camera.nearClipPlane + GUIOffset));
		SetProgram(PROGRAM.QUADS, GUIMaterial);
		Vector3 dir = end-start;
		Vector3 ortho = width/2f * (Quaternion.AngleAxis(90f, camera.transform.forward) * dir).normalized;
		GL.Color(color);
        GL.Vertex(start+ortho);
		GL.Vertex(start-ortho);
		GL.Vertex(end-ortho);
		GL.Vertex(end+ortho);
    }

	public static void DrawGUIQuad(float x, float y, float width, float height, Color color) {
		if(Return()) {return;}
		x *= Screen.width;
		y *= Screen.height;
		width *= Screen.width;
		height *= Screen.height;
		Camera camera = GetCamera();
		SetProgram(PROGRAM.QUADS, GUIMaterial);
		GL.Color(color);
		GL.Vertex(camera.ScreenToWorldPoint(new Vector3(x, y, camera.nearClipPlane + GUIOffset)));
		GL.Vertex(camera.ScreenToWorldPoint(new Vector3(x+width, y, camera.nearClipPlane + GUIOffset)));
		GL.Vertex(camera.ScreenToWorldPoint(new Vector3(x+width, y+height, camera.nearClipPlane + GUIOffset)));
		GL.Vertex(camera.ScreenToWorldPoint(new Vector3(x, y+height, camera.nearClipPlane + GUIOffset)));
	}

	public static void DrawGUITriangle(float xA, float yA, float xB, float yB, float xC, float yC, Color color) {
		if(Return()) {return;}
		xA *= Screen.width;
		yA *= Screen.height;
		xB *= Screen.width;
		yB *= Screen.height;
		xC *= Screen.width;
		yC *= Screen.height;
		Camera camera = GetCamera();
		SetProgram(PROGRAM.TRIANGLES, GUIMaterial);
		GL.Color(color);
		GL.Vertex(camera.ScreenToWorldPoint(new Vector3(xA, yA, camera.nearClipPlane + GUIOffset)));
		GL.Vertex(camera.ScreenToWorldPoint(new Vector3(xB, yB, camera.nearClipPlane + GUIOffset)));
		GL.Vertex(camera.ScreenToWorldPoint(new Vector3(xC, yC, camera.nearClipPlane + GUIOffset)));
	}

	public static void DrawGUICircle(float xCenter, float yCenter, float radius, Color color) {
		if(Return()) {return;}
		xCenter *= Screen.width;
		yCenter *= Screen.height;
		radius *= Screen.height;
		int resolution = 30;
		Camera camera = GetCamera();
		SetProgram(PROGRAM.TRIANGLES, GUIMaterial);
        GL.Color(color);
		Vector3 center = camera.ScreenToWorldPoint(new Vector3(xCenter, yCenter, camera.nearClipPlane + GUIOffset));
		for(int i=0; i<resolution-1; i++) {
			GL.Vertex(center);
			float a = 2f * Mathf.PI * (float)i / ((float)resolution-1f);
			GL.Vertex(camera.ScreenToWorldPoint(new Vector3(xCenter + Mathf.Cos(a)*radius, yCenter + Mathf.Sin(a)*radius, camera.nearClipPlane + GUIOffset)));
			float b = 2f * Mathf.PI * (float)(i+1) / ((float)resolution-1f);
			GL.Vertex(camera.ScreenToWorldPoint(new Vector3(xCenter + Mathf.Cos(b)*radius, yCenter + Mathf.Sin(b)*radius, camera.nearClipPlane + GUIOffset)));
		}
	}
	
	//------------------------------------------------------------------------------------------
	//UTILITY FUNCTIONS
	//------------------------------------------------------------------------------------------
	private static Camera GetCamera() {
		if(Camera.current != null) {
			return Camera.current;
		} else {
			return Camera.main;
		}
	}

	private static Vector3[] GetCatmullRomSpline(Vector3[] controlPoints, int sampling) {
		int cp = controlPoints.Length;
		int s = sampling;
		Vector3[] points = new Vector3[cp*s];
		for(int pos=0; pos<cp; pos++) {
			Vector3 p0 = controlPoints[(int)Mathf.Repeat(pos-1, controlPoints.Length)];
			Vector3 p1 = controlPoints[pos];
			Vector3 p2 = controlPoints[(int)Mathf.Repeat(pos+1, controlPoints.Length)];
			Vector3 p3 = controlPoints[(int)Mathf.Repeat(pos+2, controlPoints.Length)];
			for(int i=1; i<=s; i++) {
				float t = i / (float)(s-1);
				points[pos * s + i - 1] = GetCatmullRomPosition(t, p0, p1, p2, p3);
			}
		}
		return points;
	}

	private static Vector3 GetCatmullRomPosition(float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3) {
		Vector3 a = 2f * p1;
		Vector3 b = p2 - p0;
		Vector3 c = 2f * p0 - 5f * p1 + 4f * p2 - p3;
		Vector3 d = -p0 + 3f * p1 - 3f * p2 + p3;
		Vector3 pos = 0.5f * (a + (b * t) + (c * t * t) + (d * t * t * t));
		return pos;
	}

	private static bool Return() {
		if(!Drawing) {
			Debug.Log("Drawing has not been started yet.");
			return true;
		} else {
			return false;
		}
		//return !Drawing;
	}

}