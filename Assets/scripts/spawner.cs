using System.Xml.Serialization;
using System.IO;
using System.Collections.Generic;
using UnityEngine;

public class SPAWNER : MonoBehaviour
{
    [SerializeField] List<GameObject> collectible_prefabs;
    [SerializeField] List<GameObject> targetable_prefabs;

    //constants
    public const string image_directory_path = "images";

    void Start()
    {
        this.load_configuration("configuration.xml");
    }

    void load_configuration(string relative_path)
    {
        string path = Path.Combine(Application.persistentDataPath, relative_path);
        
        if (!File.Exists(path))
        {
            Debug.LogError($"configuration not found at '{path}'");
            return;
        }

        XmlSerializer serializer = new XmlSerializer(typeof(XML_CONFIGURATION));

        using (FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read))
        {
            XML_CONFIGURATION configuration = (XML_CONFIGURATION)serializer.Deserialize(stream);

            foreach (var xml_targetable in configuration.targetables)
            {
                GameObject prefab = this.targetable_prefabs.Find(p => p.name == xml_targetable.prefab_name);
                if (prefab == null)
                {
                    Debug.LogWarning($"prefab with name '{xml_targetable.prefab_name}' was not found");
                    continue;
                }
            
                GameObject targetable_object = Instantiate(prefab, xml_targetable.spawn_position, Quaternion.Euler(xml_targetable.spawn_rotation));
                if (targetable_object == null)
                {
                    Debug.LogWarning($"failed to instantiate prefab with name '{xml_targetable.prefab_name}'");
                    continue;
                }

                TARGETABLE targetable = targetable_object.GetComponent<TARGETABLE>();
                targetable.id = xml_targetable.id;
            }

            foreach (var xml_item in configuration.items)
            {
                GameObject prefab = this.collectible_prefabs.Find(p => p.name == xml_item.prefab_name);
                if (prefab == null)
                {
                    Debug.LogWarning($"prefab with name '{xml_item.prefab_name}' was not found");
                    continue;
                }

                ITEM item = ScriptableObject.CreateInstance<ITEM>();
                item.prefab = prefab;
                item.sprite = this.load_sprite(xml_item.image_path);
                item.description = xml_item.description;

                foreach (var xml_collectible in xml_item.collectibles)
                {
                    GameObject collectible_object = Instantiate(prefab, xml_collectible.spawn_position, Quaternion.Euler(xml_collectible.spawn_rotation));
                    if (collectible_object == null)
                    {
                        Debug.LogWarning($"failed to spawn an instance of prefab with name '{xml_item.prefab_name}'");
                        continue;
                    }

                    COLLECTIBLE collectible = collectible_object.GetComponent<COLLECTIBLE>();
                    collectible.item = item;
                    collectible.unique.targetable_id = xml_collectible.targetable_id;

                    if (xml_collectible.target_positionSpecified && xml_collectible.target_rotationSpecified)
                    {
                        collectible.unique.should_despawn = false;
                        collectible.unique.target_position = xml_collectible.target_position;
                        collectible.unique.target_rotation = Quaternion.Euler(xml_collectible.target_rotation);
                    }
                    else if (!xml_collectible.target_positionSpecified && !xml_collectible.target_rotationSpecified)
                    {
                        collectible.unique.should_despawn = true;
                    }
                    else
                    {
                        Debug.LogWarning($"prefab with name '{xml_item.prefab_name}' has invalid instance definition");
                        continue;
                    }
                }
            }
        }
    }

    /*
    void load_configuration(string relative_path)
    {
        string xmlPath = Path.Combine(Application.persistentDataPath, relative_path);
        string xsdPath = Path.Combine(Application.streamingAssetsPath, "configuration.xsd");

        XmlReaderSettings settings = new XmlReaderSettings();
        settings.Schemas.Add(null, xsdPath);
        settings.ValidationType = ValidationType.Schema;
        
        // This handler catches "Invalid" data (mismatched tags)
        settings.ValidationEventHandler += (sender, e) => {
            Debug.LogError($"XML Validation {e.Severity}: {e.Message}");
        };

        using (XmlReader reader = XmlReader.Create(xmlPath, settings))
        {
            try 
            {
                XmlSerializer serializer = new XmlSerializer(typeof(XML_CONFIGURATION));
                XML_CONFIGURATION configuration = (XML_CONFIGURATION)serializer.Deserialize(reader);
                // Process as normal...
            }
            catch (System.Exception ex)
            {
                Debug.LogError("Failed to deserialize: " + ex.Message);
            }
        }
    }
    */

    public Sprite load_sprite(string relative_path)
    {
        string path = Path.Combine(Application.persistentDataPath, image_directory_path, relative_path);
        if (!File.Exists(path))
        {
            Debug.LogWarning($"image not found at '{path}'");
            return null;
        }
        byte[] data = File.ReadAllBytes(path);
        //fix (apparently not garbage collected)
        Texture2D texture = new Texture2D(1, 1);
        if (!texture.LoadImage(data))
        {
            Debug.LogWarning($"failed to load image at '{path}'");
            return null;
        }
        return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
    }
}

[XmlRoot("configuration")]
public class XML_CONFIGURATION
{
    [XmlArray("items")]
    [XmlArrayItem("item")]
    public List<XML_ITEM> items = new List<XML_ITEM>();

    [XmlArray("targetables")]
    [XmlArrayItem("targetable")]
    public List<XML_TARGETABLE> targetables = new List<XML_TARGETABLE>();
}

public class XML_ITEM
{
    [XmlAttribute("prefab_name")]
    public string prefab_name;

    [XmlAttribute("description")]
    public string description;

    [XmlAttribute("image_path")]
    public string image_path;

    [XmlElement("collectible")]
    public List<XML_COLLECTIBLE> collectibles = new List<XML_COLLECTIBLE>();
}

public class XML_COLLECTIBLE
{
    [XmlAttribute("targetable_id")]
    public int targetable_id;

    [XmlElement("spawn_position")]
    public Vector3 spawn_position;

    [XmlElement("spawn_rotation")]
    public Vector3 spawn_rotation;

    [XmlIgnore] 
    public bool target_positionSpecified; 

    [XmlElement("target_position")]
    public Vector3 target_position;

    [XmlIgnore]
    public bool target_rotationSpecified;

    [XmlElement("target_rotation")]
    public Vector3 target_rotation;
}

public class XML_TARGETABLE
{
    [XmlAttribute("id")]
    public int id;

    [XmlAttribute("prefab_name")]
    public string prefab_name;

    [XmlElement("spawn_position")]
    public Vector3 spawn_position;

    [XmlElement("spawn_rotation")]
    public Vector3 spawn_rotation;
}
