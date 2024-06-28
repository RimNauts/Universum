import os
import sys
import xml.etree.ElementTree


def main():
    project_folder_path = os.path.dirname(os.path.dirname(__file__))
    about_file_path = os.path.join(project_folder_path, "About", "About.xml")

    xml_tree = xml.etree.ElementTree.parse(about_file_path)
    xml_root = xml_tree.getroot()
    xml_element = xml_root.find("modVersion")

    major_version, minor_version, patch_version = xml_element.text.split(".")

    update_type = sys.argv[1:][0]

    if update_type == "major_update":
        major_version = int(major_version) + 1
        minor_version = 0
        patch_version = 0
    elif update_type == "minor_update":
        minor_version = int(minor_version) + 1
        patch_version = 0
    elif update_type == "patch_update":
        patch_version = int(patch_version) + 1

    
    xml_element.text = f"{major_version}.{minor_version}.{patch_version}"

    xml_tree.write(about_file_path, encoding="utf-8", xml_declaration=True, short_empty_elements=False)


if __name__ == "__main__":
    main()
