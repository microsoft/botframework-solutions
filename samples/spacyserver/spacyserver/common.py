#!/usr/bin/env python
# coding: utf8

import os
import json
from werkzeug.utils import secure_filename

Root = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..")
OutputDir = os.path.join(Root, "output-apps")

def get_app_dir(id):
    return os.path.join(OutputDir, id)

def get_category_dir(app_dir):
    return os.path.join(app_dir, "category")

def get_entity_dir(app_dir):
    return os.path.join(app_dir, "entity")

def load_json(lu_data, files, output_dir):
    JsonFile = os.path.join(output_dir, "temp.json")
    if os.path.exists(JsonFile):
        os.remove(JsonFile)

    if not files:
        LuFile = os.path.join(output_dir, "temp.lu")
        with open(LuFile, 'wb') as f:
            f.write(lu_data)
    else:
        entry = files.get('entry')
        if entry:
            LuFile = os.path.join(output_dir, secure_filename(entry.filename))

        for file in files.items(True):
            fileName = os.path.join(output_dir, secure_filename(file[1].filename))
            file[1].save(fileName)
            if not LuFile:
                LuFile = fileName

    cmd = "bf luis:convert --in {0} --out {1}".format(LuFile, JsonFile)
    os.system(cmd)

    with open(JsonFile) as f:
        return json.load(f)

def save_json(obj, output_dir):
    JsonFile = os.path.join(output_dir, "last.json")
    with open(JsonFile, 'w') as f:
        json.dump(obj, f)

def remove_json(output_dir):
    JsonFile = os.path.join(output_dir, "last.json")
    if os.path.exists(JsonFile):
        os.remove(JsonFile)

def compare_json(obj, output_dir):
    JsonFile = os.path.join(output_dir, "last.json")
    if not os.path.exists(JsonFile):
        return False

    with open(JsonFile) as f:
        last = json.load(f)

    return json.dumps(obj, sort_keys=True) == json.dumps(last, sort_keys=True)
