using System.IO;
using System.CodeDom;
using System.CodeDom.Compiler;

using System.Collections.Generic;

using UnityEngine;
using UnityEditor;

using Microsoft.CSharp;


/// <summary>
/// Enumファイル生成クラス.
/// </summary>
public class EnumFileGenerator : EditorWindow
{
    public enum ValueType
    {
        Increment,   // インクリメント.
        BitFlag,     // ビットフラグ.
        FileHash,    // ハッシュ値.
    }

    // Enumメンバ一覧.
    [SerializeField] List<string> m_members = new List<string>(){ "Value1", "Value2", "Value3" };

    // Enum名.
    string m_enumName = "HogeType"; 
    // 名前空間.
    string m_namespace = "TestNameSpace";
    // 特定フォルダ以下でリストアップしたファイル名を元にメンバ生成.
    string m_listupFileRootPath = "Assets/Resources/Prefabs/";
    // 検索パターン.
    string m_searchPattern = "*.prefab";
    // Enumファイル出力先.
    string m_exportPath = "Assets/Generated";
    // タイプ.
    ValueType m_valueType = ValueType.Increment;
    // ファイル名をメンバにする.
    bool m_useValueFromFiles = false;


    /// <summary>
    /// .
    /// </summary>
    [MenuItem("Tools/Open EnumFileGenerator")]
    public static void ShowWindow()
    {
        var area = GetWindow<EnumFileGenerator>("EnumFileGenerator");
    }

    /// <summary>
    /// .
    /// </summary>
    private void OnGUI()
    {
        GUILayout.Label( "Enum型名" );
        m_enumName = GUILayout.TextField( m_enumName );

        GUILayout.Label( "名前空間" );
        m_namespace = EditorGUILayout.TextField( m_namespace );

        GUILayout.Label( "列挙タイプ" );
        m_valueType = ( ValueType )EditorGUILayout.EnumPopup( m_valueType );
        GUILayout.Space( 10 );

        m_useValueFromFiles = GUILayout.Toggle( m_useValueFromFiles, "フォルダ内のファイル名をメンバとして使う" );
        if( m_useValueFromFiles )
        {
            GUILayout.BeginVertical( GUI.skin.box );
            {
                GUILayout.Label( "対象フォルダ" );
                m_listupFileRootPath = GUILayout.TextField( m_listupFileRootPath );

                GUILayout.Label( "検索パターン" );
                m_searchPattern = GUILayout.TextField( m_searchPattern );

                if( GUILayout.Button( "リストアップ" ))
                {
                    m_members.Clear();

                    string[] fileNames = Directory.GetFiles( m_listupFileRootPath, m_searchPattern );
                    foreach( var fileName in fileNames ){
                        m_members.Add( Path.GetFileNameWithoutExtension( fileName ));
                    }
                }
            }
            GUILayout.EndVertical();
        }

        var so = new SerializedObject(this);
        EditorGUILayout.PropertyField( so.FindProperty( "m_members" ), true );
        so.ApplyModifiedProperties();
        GUILayout.Space( 10 );

        GUILayout.Label( "出力先フォルダ" );
        m_exportPath = GUILayout.TextField( m_exportPath );
        GUILayout.Space( 10 );

        if( GUILayout.Button( "出力" )){
            Export();
        }
    }

    /// <summary>
    /// .
    /// </summary>
    private void Export()
    {
        try
        {
            string savePath = Path.Combine( m_exportPath, m_enumName + ".cs" );

            using( var writer = new StreamWriter( File.Open( savePath, FileMode.Create )))
            {
                var compileUnit = new CodeCompileUnit();

                // 名前空間.
                var codeNamespace = new CodeNamespace( m_namespace );
                compileUnit.Namespaces.Add( codeNamespace );

                // Enum宣言.
                var codeEnum = new CodeTypeDeclaration( m_enumName );
                codeEnum.IsEnum = true;
                codeNamespace.Types.Add( codeEnum );

                // Enumメンバー追加.
                int counter = 0;
                foreach( var member in m_members )
                {                        
                    int value = 0;
                    switch( m_valueType )
                    {
                        case ValueType.Increment: value = counter++; break;
                        case ValueType.BitFlag: value = (1 << counter++); break;
                        case ValueType.FileHash: value = Animator.StringToHash( member ); break;
                    }
                    
                    var cm = new CodeMemberField( codeEnum.Name, member );
                    cm.InitExpression = new CodePrimitiveExpression( value );

                    // 追加.
                    codeEnum.Members.Add( cm );
                }

                // 出力設定.
                var provider = new CSharpCodeProvider();
                var options = new CodeGeneratorOptions();
                options.BlankLinesBetweenMembers = false; // 各メンバ間に空行を挿入しない.

                // 出力.
                provider.GenerateCodeFromCompileUnit( compileUnit, writer, options );                    
            }

            // AssetDatabase更新.
            AssetDatabase.ImportAsset( savePath );
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog( "Info", "出力成功", "OK" );
        }
        catch( System.Exception ex )
        {
            Debug.Assert( false, ex.Message );
            EditorUtility.DisplayDialog( "Error!!", "出力に失敗しました。\n" + ex.Message, "OK" );
        }
    }


}   // End of class EnumGenerator.

