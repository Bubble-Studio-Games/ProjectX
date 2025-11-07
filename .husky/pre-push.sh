#!/bin/sh
# ===============================================================
# pre-push: 대용량 및 특정 확장자 파일을 자동으로 DVC로 이관
# - pre-commit에서 누락된 파일을 2차로 검사
# - Git 추적 중인 파일은 자동으로 untrack 후 DVC로 전환
# - 대용량 에셋의 .meta 파일만 DVC로 추가
# - DVC push 자동 수행
# ===============================================================

LIMIT=104857600  # 100MB
found=false

echo "[husky] pre-push running" >&2

# 1) 커밋에 포함된 파일 검사
git diff --cached --name-only -z --diff-filter=AM | \
while IFS= read -r -d '' file; do
    if [ -f "$file" ]; then
        ext="${file##*.}"
        size=$(wc -c <"$file")
        process=false

        # 확장자 규칙 적용
        case "$ext" in
            fbx|anim|wav|mp4|psd|exr|hdr|tga)  # 무조건 DVC
                process=true ;;
            png|jpg|obj|blend|prefab)          # 조건부: 100MB 초과
                [ $size -gt $LIMIT ] && process=true ;;
        esac

        if [ "$process" = true ]; then
            echo "⚠️  DVC 대상 파일 감지: $file (size: $size bytes)"

            # 이미 DVC 추적 중인지 확인 (.dvc 존재 시 skip)
            if [ -f "$file.dvc" ]; then
                echo "ℹ️  Already tracked by DVC (.dvc exists): $file → skip"
                continue
            fi

            # Git이 이미 추적 중이면 자동 untrack
            if git ls-files --error-unmatch "$file" >/dev/null 2>&1; then
                echo "⚠️  Git에서 이미 추적 중: $file → 추적 해제 후 DVC로 전환"
                git rm --cached "$file"
            fi

            # dvc add 실행
            if dvc add "$file"; then
                echo "✅  dvc add succeeded for $file"

                # 혹시 남아 있는 Git 추적이 있으면 한 번 더 해제
                if git ls-files --cached --error-unmatch "$file" >/dev/null 2>&1; then
                    git rm --cached "$file"
                fi

                # .dvc 파일 및 .gitignore 자동 스테이징
                [ -f "$file.dvc" ] && git add "$file.dvc"
                dir=$(dirname "$file")
                [ -f "$dir/.gitignore" ] && git add "$dir/.gitignore"

                # --- 매칭되는 .meta 자동 DVC 등록 ---
                metafile="${file}.meta"
                if [ -f "$metafile" ] && [ ! -f "$metafile.dvc" ]; then
                    echo "🧩 Matching META found → DVC add: $metafile"
                    dvc add "$metafile" >/dev/null 2>&1 && git add "$metafile.dvc"
                fi

                found=true
            else
                echo "❌  dvc add failed for $file, skipping..."
            fi
        fi
    fi
done

# ❌ 전체 .meta 스캔 제거됨 (용량 폭증 방지)

# 2) 대형 파일이 걸렸으면 push 중단
if [ "$found" = true ]; then
    echo "❌  Push aborted. Some files were moved to DVC."
    echo "   Please commit again (.dvc + .gitignore now included) before pushing."
    exit 1
fi

# 3) DVC push 실행
echo "🚀  Running DVC push..."
if dvc push; then
    echo "✅  DVC push completed successfully."
    exit 0
else
    echo "❌  DVC push failed. Push aborted."
    exit 1
fi
