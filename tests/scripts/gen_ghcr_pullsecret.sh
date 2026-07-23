#!/usr/bin/env bash
set -euo pipefail

usage() {
    echo "Usage: $0 <github_username> <token>"
    echo
    echo "Paste the output into the 'src/kubernetes/dockerconfigsecret.yaml'."
    exit 1
}

if [ $# -ne 2 ]; then
    usage
fi

USERNAME="$1"
TOKEN="$2"

AUTH_B64=$(printf '%s:%s' "$USERNAME" "$TOKEN" | base64 -w0)
DOCKER_CONFIG="{\"auths\":{\"ghcr.io\":{\"auth\":\"${AUTH_B64}\"}}}"
printf '%s' "$DOCKER_CONFIG" | base64 -w0
echo
