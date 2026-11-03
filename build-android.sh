#!/usr/bin/env bash
set -euo pipefail
project_dir="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
editor_version="$(awk '/^m_EditorVersion:/ { print $2; exit }' "$project_dir/ProjectSettings/ProjectVersion.txt")"
editor_path="${UNITY_EDITOR:-/Applications/Unity/Hub/Editor/$editor_version/Unity.app/Contents/MacOS/Unity}"
if [[ ! -x "$editor_path" ]]; then
  echo "未找到工程指定的 Unity $editor_version 编辑器：$editor_path" >&2
  echo "编辑器在其他路径时：UNITY_EDITOR=/完整路径/Unity ./build-android.sh" >&2
  exit 2
fi
mkdir -p "$project_dir/Builds"
"$editor_path" -batchmode -quit -projectPath "$project_dir" -buildTarget Android \
  -executeMethod SpatialCatCafe.Editor.CafeBuild.Android \
  -logFile "$project_dir/Builds/android-build.log"
echo "APK：$project_dir/Builds/SpatialCatCafe.apk"
