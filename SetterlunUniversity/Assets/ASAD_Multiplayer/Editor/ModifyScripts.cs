 using UnityEngine;
using UnityEditor;
using System.IO;                                    //for modifying files (reading and writing)
using System.Collections.Generic;                   //For Lists

public class ModifyScripts : EditorWindow
{
    public class M_FileData
    {
        public bool exists = false;
        public string path = "";
    }
    public class M_Additions
    {
        public string target = "";
        public string add = "";
        public string nextline = "";
        public M_FileAddtionType type = M_FileAddtionType.NewLine;
    }

    #region Editor Variables
    GUISkin skin;
    Vector2 rect = new Vector2(400, 180);
    Vector2 maxrect = new Vector2(400, 180);
    private bool _executed = false;
    public enum M_FileAddtionType { Replace, NewLine, InsertLine }
    private int _index = 0;
    #endregion

    #region List Of Available Files 
    M_FileData _aiAnimator;
    M_FileData _aiMotor;
    M_FileData _aiWeaponsControl;
    M_FileData _meleeClickToMove;
    M_FileData _meleeCombatInput;
    M_FileData _thirdPersonAnimator;
    M_FileData _character;
    M_FileData _itemManager;
    M_FileData _shooterManager;
    M_FileData _bowControl;
    M_FileData _monoBehavior;
    M_FileData _aiController;
    M_FileData _hitBox;
    M_FileData _projectileControl;
    #endregion

    [MenuItem("Invector/Multiplayer/Add Multiplayer To Invector Scripts")]
    private static void M_ChangeScripts()
    {
        GetWindow<ModifyScripts>("UNet - Modify Scripts");
    }
    private void OnGUI()
    {
        if (!skin) skin = Resources.Load("skin") as GUISkin;
        GUI.skin = skin;

        this.minSize = rect;
        this.maxSize = maxrect;
        this.titleContent = new GUIContent("UNet: Multiplayer", null, "Adds multiplayer support to Invector scripts.");
        GUILayout.BeginVertical("Add Multiplayer Compatiblity To Scripts", "window");
        GUILayout.Space(35);

        GUILayout.BeginVertical("box");
        if (_executed == false)
        {
            EditorGUILayout.HelpBox("This will modify all relevant Invector scripts by adding needed lines to their scripts. This will make these script send their needed data across the network. Note: There is no automated process to undo these changes. If you wish to undo look at the \"InvectorScriptChanges.txt\" file that will contain a list of changes.", MessageType.Info);
        }
        else
        {
            EditorGUILayout.HelpBox("Done! The invector scripts will now be synced across the network. If you would like to undo these changes look at the InvectorScriptChanges.txt file to see what lines were added to what files.", MessageType.Info);
        }
        GUILayout.EndVertical();
        if (_executed == false)
        {
            if (GUILayout.Button("Add Multiplayer Lines"))
            {
                _executed = true;
                M_AddMultiplayerToScripts();
            }
        }
    }

    void M_AddMultiplayerToScripts()
    {
        M_ShooterManager();
        M_ControlAimCanvas();
        M_MeleeManager();
        M_MeleeCombatInputCS();
        M_ThirdPersonAnimatorCS();
        M_ThrowUI();
        M_AIController();
        M_HitBox();
        M_ProjectileControl();
    }

    #region Individual Files Modification Instructions
    void M_ThrowUI()
    {
        _monoBehavior = FileExists("vThrowUI.cs", Application.dataPath);
        if (_monoBehavior.exists == true)
        {
            List<M_Additions> newlines = new List<M_Additions>();
            M_Additions[] adding = new M_Additions[5];
            adding[0] = new M_Additions();
            adding[0].target = "public class vThrowUI : MonoBehaviour";
            adding[0].add = "public class vThrowUI : NetworkBehaviour";
            adding[0].type = M_FileAddtionType.Replace;
            adding[1] = new M_Additions();
            adding[1].target = "void Start()";
            adding[1].add = "protected virtual void Start()";
            adding[1].type = M_FileAddtionType.Replace;
            adding[2] = new M_Additions();
            adding[2].target = "throwManager = FindObjectOfType<vThrowObject>();";
            adding[2].add = "//throwManager = FindObjectOfType<vThrowObject>();";
            adding[2].type = M_FileAddtionType.Replace;
            adding[3] = new M_Additions();
            adding[3].target = "using UnityEngine.UI;";
            adding[3].add = "using UnityEngine.Networking;";
            adding[3].type = M_FileAddtionType.NewLine;
            adding[4] = new M_Additions();
            adding[4].target = "private void Start()";
            adding[4].add = "protected virtual void Start()";
            adding[4].type = M_FileAddtionType.Replace;

            newlines.AddRange(adding);
            ModifyFile(_monoBehavior.path, newlines);
        }
    }
    void M_ThirdPersonAnimatorCS()
    {
        _thirdPersonAnimator = FileExists("vThirdPersonAnimator.cs", Application.dataPath);
        if (_thirdPersonAnimator.exists == true)
        {
            List<M_Additions> newlines = new List<M_Additions>();
            M_Additions[] adding = new M_Additions[2];
            adding[0] = new M_Additions();
            adding[0].target = "void RandomIdle()";
            adding[0].add = "protected virtual void RandomIdle()";
            adding[0].type = M_FileAddtionType.Replace;
            adding[1] = new M_Additions();
            adding[1].target = "private float randomIdleCount, randomIdle;";
            adding[1].add = "protected float randomIdleCount, randomIdle;";
            adding[1].type = M_FileAddtionType.Replace;

            newlines.AddRange(adding);
            ModifyFile(_thirdPersonAnimator.path, newlines);
        }
    }
    void M_MeleeCombatInputCS()
    {
        _meleeCombatInput = FileExists("vMeleeCombatInput.cs", Application.dataPath);
        if (_meleeCombatInput.exists == true)
        {
            List<M_Additions> newlines = new List<M_Additions>();
            M_Additions[] adding = new M_Additions[5];
            adding[0] = new M_Additions();
            adding[0].target = "public void OnEnableAttack()";
            adding[0].add = "public virtual void OnEnableAttack()";
            adding[0].type = M_FileAddtionType.Replace;
            adding[1] = new M_Additions();
            adding[1].target = "public void OnDisableAttack()";
            adding[1].add = "public virtual void OnDisableAttack()";
            adding[1].type = M_FileAddtionType.Replace;
            adding[2] = new M_Additions();
            adding[2].target = "public void ResetAttackTriggers()";
            adding[2].add = "public virtual void ResetAttackTriggers()";
            adding[2].type = M_FileAddtionType.Replace;
            adding[3] = new M_Additions();
            adding[3].target = "public void OnRecoil(int recoilID)";
            adding[3].add = "public virtual void OnRecoil(int recoilID)";
            adding[3].type = M_FileAddtionType.Replace;
            adding[4] = new M_Additions();
            adding[4].target = "public void OnReceiveAttack(vDamage damage, vIMeleeFighter attacker)";
            adding[4].add = "public virtual void OnReceiveAttack(vDamage damage, vIMeleeFighter attacker)";
            adding[4].type = M_FileAddtionType.Replace;
            

            newlines.AddRange(adding);
            ModifyFile(_meleeCombatInput.path, newlines);
        }
    }
    void M_MeleeManager()
    {
        _meleeCombatInput = FileExists("vMeleeManager.cs", Application.dataPath);
        if (_meleeCombatInput.exists == true)
        {
            List<M_Additions> newlines = new List<M_Additions>();
            M_Additions[] adding = new M_Additions[6];
            adding[0] = new M_Additions();
            adding[0].target = "private int currentRecoilID;";
            adding[0].add = "protected int currentRecoilID;";
            adding[0].type = M_FileAddtionType.Replace;
            adding[1] = new M_Additions();
            adding[1].target = "private bool activeRagdoll;";
            adding[1].add = "protected bool activeRagdoll;";
            adding[1].type = M_FileAddtionType.Replace;
            adding[2] = new M_Additions();
            adding[2].target = "private int currentReactionID;";
            adding[2].add = "protected int currentReactionID;";
            adding[2].type = M_FileAddtionType.Replace;
            adding[3] = new M_Additions();
            adding[3].target = "private string attackName;";
            adding[3].add = "protected string attackName;";
            adding[3].type = M_FileAddtionType.Replace;
            adding[4] = new M_Additions();
            adding[4].target = "private bool ignoreDefense;";
            adding[4].add = "protected bool ignoreDefense;";
            adding[4].type = M_FileAddtionType.Replace;
            adding[5] = new M_Additions();
            adding[5].target = "private int damageMultiplier;";
            adding[5].add = "protected int damageMultiplier;";
            adding[5].type = M_FileAddtionType.Replace;

            newlines.AddRange(adding);
            ModifyFile(_meleeCombatInput.path, newlines);
        }
    }
    void M_ControlAimCanvas()
    {
        _meleeCombatInput = FileExists("vControlAimCanvas.cs", Application.dataPath);
        if (_meleeCombatInput.exists == true)
        {
            List<M_Additions> newlines = new List<M_Additions>();
            M_Additions[] adding = new M_Additions[1];
            adding[0] = new M_Additions();
            adding[0].target = "protected vThirdPersonController cc;";
            adding[0].add = "public void SetCharacterController(vThirdPersonController controller) { cc = controller; }";
            adding[0].nextline = "protected UnityEvent onEnableAim { get { return currentAimCanvas.onEnableAim; } }";
            adding[0].type = M_FileAddtionType.InsertLine;

            newlines.AddRange(adding);
            ModifyFile(_meleeCombatInput.path, newlines);
        }
    }
    void M_ShooterManager()
    {
        _shooterManager = FileExists("vShooterManager.cs", Application.dataPath);
        if (_shooterManager.exists == true)
        {
            List<M_Additions> newlines = new List<M_Additions>();
            M_Additions[] adding = new M_Additions[2];
            adding[0] = new M_Additions();
            adding[0].target = "public void ReloadWeapon(bool ignoreAmmo = false, bool ignoreAnim = false)";
            adding[0].add = "public virtual void ReloadWeapon(bool ignoreAmmo = false, bool ignoreAnim = false)";
            adding[0].type = M_FileAddtionType.Replace;
            adding[1] = new M_Additions();
            adding[1].target = "private float currentShotTime;";
            adding[1].add = "protected float currentShotTime;";
            adding[1].type = M_FileAddtionType.Replace;

            newlines.AddRange(adding);
            ModifyFile(_shooterManager.path, newlines);
        }
    }
    void M_AIController()
    {
        _aiController = FileExists("v_AIController.cs", Application.dataPath);
        if (_aiController.exists == true)
        {
            List<M_Additions> newlines = new List<M_Additions>();
            M_Additions[] adding = new M_Additions[1];
            adding[0] = new M_Additions();
            adding[0].target = "public void OnReceiveAttack(vDamage damage, vIMeleeFighter attacker)";
            adding[0].add = "public virtual void OnReceiveAttack(vDamage damage, vIMeleeFighter attacker)";
            adding[0].type = M_FileAddtionType.Replace;

            newlines.AddRange(adding);
            ModifyFile(_aiController.path, newlines);
        }
    }
    void M_HitBox()
    {
        _hitBox = FileExists("vHitBox.cs", Application.dataPath);
        if (_hitBox.exists == true)
        {
            List<M_Additions> newlines = new List<M_Additions>();
            M_Additions[] adding = new M_Additions[2];
            adding[0] = new M_Additions();
            adding[0].target = "void OnTriggerEnter(Collider other)";
            adding[0].add = "protected virtual void OnTriggerEnter(Collider other)";
            adding[0].type = M_FileAddtionType.Replace;
            adding[1] = new M_Additions();
            adding[1].target = "bool TriggerCondictions(Collider other)";
            adding[1].add = "protected bool TriggerCondictions(Collider other)";
            adding[1].type = M_FileAddtionType.Replace;

            newlines.AddRange(adding);
            ModifyFile(_hitBox.path, newlines);
        }
    }
    void M_ProjectileControl()
    {
        _projectileControl = FileExists("vProjectileControl.cs", Application.dataPath);
        if (_projectileControl.exists == true)
        {
            List<M_Additions> newlines = new List<M_Additions>();
            M_Additions[] adding = new M_Additions[2];
            adding[0] = new M_Additions();
            adding[0].target = "hitInfo.collider.gameObject.ApplyDamage(damage, damage.sender.GetComponent<vIMeleeFighter>());";
            adding[0].add = "if (GetComponent<NetworkIdentity>().isLocalPlayer == true) { GetComponent<NetworkEvents>().Cmd_ServerSendDamage(JsonUtility.ToJson(damage)); }";
            adding[0].type = M_FileAddtionType.NewLine;
            adding[1] = new M_Additions();
            adding[1].target = "using System.Collections.Generic;";
            adding[1].add = "using UnityEngine.Networking;";
            adding[1].type = M_FileAddtionType.NewLine;

            newlines.AddRange(adding);
            ModifyFile(_projectileControl.path, newlines);
        }
    }
    #endregion

    #region Modification Logic
    M_FileData FileExists(string filename, string directory)
    {
        M_FileData data = new M_FileData();
        DirectoryInfo dir = new DirectoryInfo(directory);
        foreach (FileInfo file in dir.GetFiles("*.cs"))
        {
            if (file.Name == filename)
            {
                data.exists = true;
                data.path = file.ToString();
                return data;
            }
        }
        foreach(string subDir in Directory.GetDirectories(directory))
        {
            data = FileExists(filename, subDir);
            if (data.exists == true)
            {
                return data;
            }
        }
        data.exists = false;
        data.path = "";
        return data;
    }
    void ModifyFile(string filepath, List<M_Additions> additions)
    {
        List<string> lines = new List<string>(System.IO.File.ReadAllLines(filepath));
        List<string> modified = new List<string>();
        bool added = false;

        for (int i=0; i < lines.Count; i++)
        {
            foreach (M_Additions item in additions)
            {
                if (lines[i].Trim().Equals(item.target))
                {
                    string space;
                    switch (item.type)
                    {
                        case M_FileAddtionType.NewLine:
                            added = true;
                            modified.Add(lines[i]);
                            if (!lines[i + 1].Trim().Equals(item.add))              //prevent this code from adding the same line in twice
                            {
                                space = lines[i].Split(item.target[0])[0];          //Get spaces
                                modified.Add(space + item.add);
                            }
                            break;
                        case M_FileAddtionType.Replace:
                            added = true;
                            if (!lines[i].Trim().Equals("//"+item.target))         //prevent this code from adding the same line in twice
                            {
                                space = lines[i].Split(item.target[0])[0];         //Get spaces
                                modified.Add(space+"//"+lines[i].Trim());          //Comment out the target line
                            }
                            else
                            {
                                modified.Add(lines[i]);
                            }
                            if (!lines[i+1].Trim().Equals(item.add))               //prevent this code from adding the same line in twice
                            {
                                space = lines[i].Split(item.target[0])[0];         //Get spaces
                                modified.Add(space + item.add);                    //Add new line
                            }
                            break;
                        case M_FileAddtionType.InsertLine:
                            _index = i + 1;
                            if (lines[_index].Trim() == "")
                                _index += 1;
                            if (lines[_index].Trim().Equals(item.nextline))
                            {
                                added = true;
                                space = lines[i].Split(item.target[0])[0];         //Get spaces
                                modified.Add(lines[i]);
                                modified.Add(space+item.add);
                            }
                            break;
                    }
                }
            }
            if (added == false)
            {
                modified.Add(lines[i]);
            }
            else
            {
                added = false;
            }
            
        }
        
        using (StreamWriter writer = new StreamWriter(filepath, false))
        {
            foreach (string line in modified)
            {
                writer.WriteLine(line);
            }
        }
    }
    #endregion
}
