using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using TwoHandedHero;

public static class BuildTwoHandedHero
{
    const string Root = "Assets/TwoHandedHero";
    class Sheet { public Sprite[] sprites; public Vector2[] shoulders; }
    static Rect Bounds(Texture2D image, RectInt cell)
    {
        int minX=cell.xMax, minY=cell.yMax, maxX=cell.x, maxY=cell.y;
        for(int y=cell.y;y<cell.yMax;y++) for(int x=cell.x;x<cell.xMax;x++)
            if(image.GetPixel(x,y).a > .2f) { minX=Math.Min(minX,x); minY=Math.Min(minY,y); maxX=Math.Max(maxX,x); maxY=Math.Max(maxY,y); }
        if(maxX<=minX || maxY<=minY) throw new Exception("Empty sprite cell");
        return new Rect(minX,minY,maxX-minX+1,maxY-minY+1);
    }
    static Texture2D Read(string path)
    {
        var t = new Texture2D(2,2,TextureFormat.RGBA32,false);
        t.LoadImage(File.ReadAllBytes(path));
        if(!t.GetPixels32().Any(p=>p.a==0)) throw new Exception(path+" has no transparent pixels");
        return t;
    }
    static void Import(string path, SpriteMetaData[] data, float ppu)
    {
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType=TextureImporterType.Sprite; importer.spriteImportMode=SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit=ppu; importer.filterMode=FilterMode.Point;
        importer.textureCompression=TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled=false; importer.alphaIsTransparency=true;
        importer.maxTextureSize=4096; importer.npotScale=TextureImporterNPOTScale.None;
#pragma warning disable 0618
        importer.spritesheet=data;
#pragma warning restore 0618
        importer.SaveAndReimport();
    }
    // Rectangles retain their original cells; pivots use measured feet and artist-selected shoulder X.
    static Sheet Body(string name,int cols,int rows,float[] shoulderX)
    {
        string path=Root+"/Art/"+name+".png";
        var tex=Read(path); int w=tex.width/cols,h=tex.height/rows;
        var bounds=new Rect[cols*rows];
        var cells=new RectInt[bounds.Length];
        for(int i=0;i<bounds.Length;i++)
        {
            cells[i]=new RectInt(i%cols*w,(rows-1-i/cols)*h,w,h);
            // The generated stop sheet has an offset row gutter; do not include the following row's hair.
            if(name=="Stop") cells[i]=i<3 ? new RectInt(i*w,534,w,490) : new RectInt((i%3)*w,0,w,534);
            bounds[i]=Bounds(tex,cells[i]);
        }
        float ppu=bounds.Max(b=>b.height)/2.4f;
        var metadata=new SpriteMetaData[bounds.Length];
        var result=new Sheet { shoulders=new Vector2[bounds.Length] };
        for(int i=0;i<bounds.Length;i++)
        {
            int x=cells[i].x,y=cells[i].y; int cellHeight=cells[i].height;
            float sx=shoulderX[i]*w;
            metadata[i]=new SpriteMetaData {name=name+"_"+i.ToString("00"),rect=new Rect(x,y,w,cellHeight),alignment=9,
                pivot=new Vector2(sx/w,(bounds[i].yMin-y)/cellHeight)};
            result.shoulders[i]=new Vector2(0,bounds[i].height/ppu*(name=="Stop"?.71f:name=="Shoot"?.71f:.68f));
        }
        Import(path,metadata,ppu);
        result.sprites=AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().OrderBy(s=>s.name).ToArray();
        UnityEngine.Object.DestroyImmediate(tex); return result;
    }
    static AnimationClip Clip(string name,Sheet sheet,float fps,bool loop)
    {
        var clip=new AnimationClip {name=name,frameRate=fps};
        var keys=new ObjectReferenceKeyframe[sheet.sprites.Length+1];
        var y=new AnimationCurve();
        for(int i=0;i<sheet.sprites.Length;i++)
        {
            keys[i]=new ObjectReferenceKeyframe { time=i/fps,value=sheet.sprites[i] };
            y.AddKey(new Keyframe(i/fps,sheet.shoulders[i].y,float.PositiveInfinity,float.PositiveInfinity));
        }
        keys[keys.Length-1]=new ObjectReferenceKeyframe {time=sheet.sprites.Length/fps,value=sheet.sprites[loop?0:sheet.sprites.Length-1]};
        y.AddKey(new Keyframe(sheet.sprites.Length/fps,sheet.shoulders[loop?0:sheet.sprites.Length-1].y));
        AnimationUtility.SetObjectReferenceCurve(clip,new EditorCurveBinding {path="Body",type=typeof(SpriteRenderer),propertyName="m_Sprite"},keys);
        AnimationUtility.SetEditorCurve(clip,EditorCurveBinding.FloatCurve("Shoulder",typeof(Transform),"m_LocalPosition.y"),y);
        var settings=AnimationUtility.GetAnimationClipSettings(clip); settings.loopTime=loop; AnimationUtility.SetAnimationClipSettings(clip,settings);
        AssetDatabase.CreateAsset(clip,Root+"/Animations/"+name+".anim"); return clip;
    }
    static Transform Child(string name,Transform parent)
    {var go=new GameObject(name);go.transform.SetParent(parent,false);return go.transform;}
    static SpriteRenderer Render(string name,Transform parent,Sprite sprite,int order)
    {var t=Child(name,parent);var r=t.gameObject.AddComponent<SpriteRenderer>();r.sprite=sprite;r.sortingOrder=order;return r;}

    [MenuItem("Tools/Two Handed Hero/Rebuild Prototype Assets")]
    public static void Build()
    {
        AssetDatabase.Refresh();
        foreach(string dir in new[]{"Animations","Prefabs","Scenes","Weapons"}) Directory.CreateDirectory(Root+"/"+dir);
        AssetDatabase.Refresh();
        // This explicit rebuild replaces only this package's generated assets; never runs on import.
        foreach(string folder in new[]{"Animations","Prefabs","Weapons"})
            foreach(string file in Directory.GetFiles(Root+"/"+folder).Where(p=>!p.EndsWith(".meta"))) AssetDatabase.DeleteAsset(file);
        var carry=Body("Carry",4,2,new[]{.58f,.58f,.60f,.59f,.58f,.58f,.60f,.60f});
        var run=Body("Run",4,4,new[]{.64f,.64f,.65f,.64f,.64f,.64f,.64f,.65f,.64f,.64f,.66f,.65f,.64f,.65f,.65f,.65f});
        var stop=Body("Stop",3,2,new[]{.65f,.56f,.45f,.53f,.52f,.46f});
        var shoot=Body("Shoot",2,2,new[]{.56f,.40f,.60f,.48f});
        var clips=new[]{Clip("Carry",carry,8,true),Clip("Run",run,16,true),Clip("Stop",stop,12,false),Clip("Shoot",shoot,12,false)};
        var controller=AnimatorController.CreateAnimatorControllerAtPath(Root+"/Animations/TwoHandedHero.controller");
        foreach(var clip in clips) {var state=controller.layers[0].stateMachine.AddState(clip.name);state.motion=clip;}
        string partsPath=Root+"/Art/Parts.png";var partsTex=Read(partsPath);
        // Parts use tight Sprite rectangles and explicit hand/grip pivots, without modifying source pixels.
        int cw=partsTex.width/2,ch=partsTex.height/2;
        string[] names={"SupportArm","TriggerArm","PulseRifle","ArcRifle"};
        Vector2[] pivots={new Vector2(.86f,.13f),new Vector2(.91f,.49f),new Vector2(.29f,.15f),new Vector2(.29f,.15f)};
        var meta=new SpriteMetaData[4];
        for(int i=0;i<4;i++) meta[i]=new SpriteMetaData {name=names[i],rect=Bounds(partsTex,new RectInt(i%2*cw,(1-i/2)*ch,cw,ch)),alignment=9,pivot=pivots[i]};
        Import(partsPath,meta,400);
        var parts=AssetDatabase.LoadAllAssetsAtPath(partsPath).OfType<Sprite>().ToDictionary(s=>s.name);
        var hero=new GameObject("TwoHandedHero");var rig=hero.AddComponent<TwoHandedRig>();
        var visuals=Child("Visuals",hero.transform); rig.visualRoot=visuals;
        rig.bodyAnimator=visuals.gameObject.AddComponent<Animator>();rig.bodyAnimator.runtimeAnimatorController=controller;
        Render("Body",visuals,carry.sprites[0],10);
        rig.shoulder=Child("Shoulder",visuals);rig.shoulder.localPosition=carry.shoulders[0];
        rig.grip=Child("WeaponGrip",visuals);rig.grip.localPosition=carry.shoulders[0]+rig.carryGrip;
        rig.weaponRenderer=Render("Weapon",rig.grip,parts["PulseRifle"],20);
        // Standardize the rifle family to the same world width and grip spacing.
        float weaponScale=1.65f/parts["PulseRifle"].bounds.size.x;
        rig.weaponRenderer.transform.localScale=Vector3.one*weaponScale;
        var back=Render("SupportHand",rig.grip,parts["SupportArm"],5);rig.supportHand=back.transform;
        back.transform.localScale=Vector3.one*(1.5f/parts["SupportArm"].bounds.size.x);
        back.transform.localRotation=Quaternion.Euler(0,0,17);
        var front=Render("TriggerHand",rig.grip,parts["TriggerArm"],30);rig.frontHand=front.transform;
        front.transform.localScale=Vector3.one*(.85f/parts["TriggerArm"].bounds.size.x);
        rig.muzzle=Child("Muzzle",rig.grip);
        var weapons=new TwoHandedWeapon[2];
        for(int i=0;i<2;i++)
        {
            var weapon=ScriptableObject.CreateInstance<TwoHandedWeapon>();weapon.name=names[i+2];weapon.sprite=parts[weapon.name];
            weapon.supportGrip=new Vector2(.54f,.02f);weapon.muzzle=new Vector2(1.17f,.30f);
            AssetDatabase.CreateAsset(weapon,Root+"/Weapons/"+weapon.name+".asset");weapons[i]=weapon;
        }
        rig.startingWeapon=weapons[0];rig.supportHand.localPosition=weapons[0].supportGrip;rig.muzzle.localPosition=weapons[0].muzzle;
        var prefab=PrefabUtility.SaveAsPrefabAsset(hero,Root+"/Prefabs/TwoHandedHero.prefab");UnityEngine.Object.DestroyImmediate(hero);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var camGO=new GameObject("Camera");var cam=camGO.AddComponent<Camera>();cam.orthographic=true;cam.orthographicSize=3.1f;
        cam.transform.position=new Vector3(0,1.3f,-10);cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.035f,.045f,.10f);
        var instance=(GameObject)PrefabUtility.InstantiatePrefab(prefab);
        var preview=new GameObject("Preview Controls").AddComponent<TwoHandedPreview>();preview.hero=instance.GetComponent<TwoHandedRig>();preview.weapons=weapons;
        // Grid uses line geometry, keeping the sprite textures independent.
        var mat=new Material(Shader.Find("Sprites/Default"));AssetDatabase.CreateAsset(mat,Root+"/Prefabs/GridMaterial.mat");
        for(int i=-10;i<=10;i++)
        {
            var line=new GameObject("Floor "+i).AddComponent<LineRenderer>();line.sharedMaterial=mat;line.positionCount=2;
            line.startWidth=line.endWidth=.01f;line.startColor=line.endColor=new Color(.08f,.25f,.32f);
            line.SetPosition(0,new Vector3(i,-.1f,0));line.SetPosition(1,new Vector3(i,-.3f,0));
        }
        var floor=new GameObject("Floor baseline").AddComponent<LineRenderer>();floor.sharedMaterial=mat;floor.positionCount=2;
        floor.startWidth=floor.endWidth=.02f;floor.startColor=floor.endColor=Color.cyan;
        floor.SetPosition(0,new Vector3(-20,-.02f,0));floor.SetPosition(1,new Vector3(20,-.02f,0));
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),Root+"/Scenes/TwoHandedPreview.unity");
        AssetDatabase.SaveAssets();
        Debug.Log("TWO_HANDED_BUILD_OK: 34 body frames, 4 clips, 2 weapons, prefab and preview scene.");
    }
}
