#!/usr/bin/env bash
# バージョン番号を更新してコミット・タグ付け・pushし、インストーラーを作成するスクリプト
# 使い方: echo "2.12.0.15" | bat/bump-version.sh
set -euo pipefail

# スクリプトの場所を基準にリポジトリのルートへ移動する
cd "$(dirname "$0")/.."

CSPROJ="AudioTranscription/AudioTranscription.csproj"
WXS="WiXToolset/Package.wxs"
INSTALLER_BAT_DIR="bat"
MSI="WiXToolset/AudioTranscription.msi"

# 必要なファイルが存在するか確認する
for FILE in "$CSPROJ" "$WXS" "${INSTALLER_BAT_DIR}/create-installer.bat"; do
  if [[ ! -f "$FILE" ]]; then
    echo "エラー: ${FILE} が見つかりません。" >&2
    exit 1
  fi
done

# 標準入力からバージョン番号を読み取る（例: 2.12.0.15）
if ! IFS= read -r -p "新しいバージョンを入力してください (例: 2.12.0.15): " VERSION; then
  echo "エラー: 標準入力からバージョン番号を入力してください（例: 2.12.0.15）。" >&2
  exit 1
fi

# Windows 由来の CR や余分な空白を除去する
VERSION="${VERSION%$'\r'}"
VERSION="${VERSION//[[:space:]]/}"

if [[ ! "$VERSION" =~ ^[0-9]+\.[0-9]+\.[0-9]+\.[0-9]+$ ]]; then
  echo "エラー: バージョン番号は 2.12.0.15 のような形式で指定してください: '${VERSION}'" >&2
  exit 1
fi

# 末尾のビルド番号を除いたバージョン（例: 2.12.0.15 -> 2.12.0）
BASE_VERSION="${VERSION%.*}"
TAG="v${BASE_VERSION}"

# Git リポジトリのブランチ上で実行されていることを確認する
if ! git rev-parse --is-inside-work-tree >/dev/null 2>&1; then
  echo "エラー: Git リポジトリ内で実行してください。" >&2
  exit 1
fi

if ! BRANCH="$(git symbolic-ref --quiet --short HEAD)"; then
  echo "エラー: Git のブランチ上で実行してください。" >&2
  exit 1
fi

# 既に同じタグが存在する場合は中断する
if git rev-parse -q --verify "refs/tags/${TAG}" >/dev/null; then
  echo "エラー: タグ ${TAG} は既に存在します。" >&2
  exit 1
fi

REMOTE_TAG="$(git ls-remote --tags origin "refs/tags/${TAG}" 2>/dev/null)" || {
  echo "エラー: origin に接続できません。" >&2
  exit 1
}

if [[ -n "$REMOTE_TAG" ]]; then
  echo "エラー: タグ ${TAG} は origin に既に存在します。" >&2
  exit 1
fi

# インストーラーの作成に必要な Windows 環境を確認する
if ! command -v cmd.exe >/dev/null 2>&1; then
  echo "エラー: cmd.exe が見つかりません。WSL または Git Bash から実行してください。" >&2
  exit 1
fi

if command -v wslpath >/dev/null 2>&1; then
  REPO_DIR_WIN="$(wslpath -w "$PWD")"
elif command -v cygpath >/dev/null 2>&1; then
  REPO_DIR_WIN="$(cygpath -w "$PWD")"
else
  echo "エラー: WSL または Git Bash から実行してください。" >&2
  exit 1
fi

echo "バージョン ${VERSION} に更新します（タグ: ${TAG}）。"

# バージョン番号を更新する
sed -i -E "s|<AssemblyVersion>[^<]*</AssemblyVersion>|<AssemblyVersion>${VERSION}</AssemblyVersion>|" "$CSPROJ"
sed -i -E "s|<FileVersion>[^<]*</FileVersion>|<FileVersion>${VERSION}</FileVersion>|" "$CSPROJ"
sed -i -E "s|(<Package[[:space:]][^>]*Version=\")[^\"]*(\")|\1${VERSION}\2|" "$WXS"

# 置換できたか確認する
if ! grep -Fq "<AssemblyVersion>${VERSION}</AssemblyVersion>" "$CSPROJ" \
  || ! grep -Fq "<FileVersion>${VERSION}</FileVersion>" "$CSPROJ" \
  || ! grep -Fq "Version=\"${VERSION}\"" "$WXS"; then
  echo "エラー: バージョン番号の更新に失敗しました。" >&2
  exit 1
fi

# バージョン番号に変更がない場合は中断する
if git diff --quiet HEAD -- "$CSPROJ" "$WXS"; then
  echo "エラー: バージョンは既に ${VERSION} です。変更がありません。" >&2
  exit 1
fi

# バージョン番号のファイルだけをコミットする
git commit -m "バージョン${BASE_VERSION}" -- "$CSPROJ" "$WXS"

# タグを付ける
git tag "$TAG"

# ブランチとタグを origin に push する
git push --atomic origin "HEAD:refs/heads/${BRANCH}" "refs/tags/${TAG}"

# 古いインストーラーが残っていると作成失敗を検知できないため、削除しておく
rm -f "$MSI"

echo "インストーラーを作成します。"
# create-installer.bat 内の pause で停止しないよう、標準入力を /dev/null にする
# WSL から cmd.exe に渡す引用符は正しく解釈されないため、パスを引用符で囲まずに渡す
cmd.exe /d /c "pushd ${REPO_DIR_WIN} && cd ${INSTALLER_BAT_DIR} && create-installer.bat" < /dev/null

if [[ ! -f "$MSI" ]]; then
  echo "エラー: インストーラー ${MSI} が見つかりません。作成に失敗した可能性があります。" >&2
  exit 1
fi

echo "インストーラーを作成しました: $PWD/$MSI"

echo "GitHub Release を追加します。"
gh release create ${TAG} --generate-notes $PWD/$MSI
