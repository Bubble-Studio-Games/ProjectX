#!/bin/sh
set -e

# --- DVC 관리 임계값: 100MB (bytes) ---
LIMIT=$((100 * 1024 * 1024))
found_large_file_to_convert=false

echo "🔍 Running pre-push DVC check (Threshold: 100MB) on push commits..."

# DVC 잠금 파일 정리 (오류 방지)
rm -rf .dvc/tmp/lock .dvc/tmp/rwlock >/dev/null 2>&1 || true

# 1. pre-push 인수를 읽어 Push되는 커밋 범위를 파악
while read local_ref local_sha remote_ref remote_sha; do
    
    # 2. Push되는 커밋 범위 내에서 변경(추가/수정)된 파일 목록을 가져옴
    # diff-filter=AM: Added, Modified 파일만 대상으로 함
    if [ "$local_sha" != "0000000000000000000000000000000000000000" ]; then
        git diff --name-only --diff-filter=AM "$remote_sha...$local_sha" | while read file; do

            # 3. .meta 파일이나 .dvc 파일은 무시하고 Git이 관리하게 둠 (가장 중요!)
            [[ "$file" == *.meta ]] && continue
            [[ "$file" == *.dvc ]] && continue
            
            # 파일이 존재하고 일반 파일인지 확인
            if [ -f "$file" ]; then
                size=$(stat -c%s "$file" 2>/dev/null || echo 0)
                
                # 4. 100MB 초과 여부 체크
                if [ $size -gt $LIMIT ]; then
                    
                    # 5. DVC로 아직 추적되지 않은 경우
                    if [ ! -f "$file.dvc" ]; then
                        
                        size_mb=$(echo "$size" | awk '{print int($1/1048576)}' 2>/dev/null || echo "N/A")
                        echo "⚠️  DVC 대상 파일 감지: $file (Size: ${size_mb}MB) -> OVER 100MB"
                        
                        # Git 추적 해제 후 DVC 등록
                        if git ls-files --error-unmatch "$file" >/dev/null 2>&1; then
                            echo "ℹ️  File tracked by Git → Untracking: $file"
                            # Git 추적만 해제하고 로컬 파일은 DVC가 가져가도록 보존
                            git rm --cached "$file"
                        fi

                        # DVC에 추가 및 .dvc 파일 Git 스테이징
                        if dvc add "$file" >/dev/null 2>&1; then
                            echo "✅  dvc add succeeded for $file"
                            git add "$file.dvc"
                            
                            # 6. 매칭되는 .meta 파일이 있다면 Git에 추가 (Git으로만 관리)
                            metafile="${file}.meta"
                            if [ -f "$metafile" ]; then
                                echo "🧩 Matching META found → Git add: $metafile"
                                git add "$metafile"
                            fi

                            found_large_file_to_convert=true
                        else
                            echo "❌  dvc add failed for $file, skipping..."
                        fi
                    fi
                fi
            fi
        done
    fi
done

# 7. DVC 전환이 발생했으면 push 중단
if [ "$found_large_file_to_convert" = true ]; then
    echo "❌ Push aborted. Some large files were found in your commits and moved to DVC."
    echo "   Please commit the staged .dvc and .meta files before trying to push again."
    exit 1
fi

# 8. DVC push 실행
echo "🚀 Running DVC push..."
if dvc push; then
    echo "✅ DVC push completed successfully."
    exit 0
else
    echo "❌ DVC push failed. Push aborted."
    exit 1
fi