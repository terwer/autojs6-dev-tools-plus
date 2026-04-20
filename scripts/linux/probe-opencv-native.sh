#!/usr/bin/env bash

set -euo pipefail

TARGET_DIR="${1:-artifacts/linux-x64}"
NATIVE_NAME="libOpenCvSharpExtern.so"

echo "[probe] target dir: ${TARGET_DIR}"

if [[ ! -d "${TARGET_DIR}" ]]; then
  echo "[probe] error: target directory does not exist: ${TARGET_DIR}" >&2
  exit 2
fi

if [[ ! -f "${TARGET_DIR}/OpenCvSharp.dll" ]]; then
  echo "[probe] error: OpenCvSharp.dll not found in publish output." >&2
  exit 3
fi

declare -a CANDIDATES=(
  "${TARGET_DIR}/${NATIVE_NAME}"
  "/usr/lib/${NATIVE_NAME}"
  "/usr/local/lib/${NATIVE_NAME}"
  "/lib/${NATIVE_NAME}"
  "/lib64/${NATIVE_NAME}"
  "/usr/lib/x86_64-linux-gnu/${NATIVE_NAME}"
  "/usr/lib64/${NATIVE_NAME}"
)

FOUND_NATIVE=""

for candidate in "${CANDIDATES[@]}"; do
  if [[ -f "${candidate}" ]]; then
    FOUND_NATIVE="${candidate}"
    break
  fi
done

if [[ -z "${FOUND_NATIVE}" ]]; then
  echo "[probe] error: ${NATIVE_NAME} was not found in the publish output or common system library paths." >&2
  echo "[probe] note: linux native runtime is intentionally not pinned by this repository yet." >&2
  exit 4
fi

echo "[probe] found native library: ${FOUND_NATIVE}"

if command -v ldd >/dev/null 2>&1; then
  echo "[probe] running ldd..."
  LDD_OUTPUT="$(ldd "${FOUND_NATIVE}" || true)"
  echo "${LDD_OUTPUT}"

  if grep -q "not found" <<< "${LDD_OUTPUT}"; then
    echo "[probe] error: unresolved native dependencies detected." >&2
    exit 5
  fi
else
  echo "[probe] warning: ldd is not available, skipped dependency-chain validation."
fi

echo "[probe] success: linux native dependency chain looks loadable."
