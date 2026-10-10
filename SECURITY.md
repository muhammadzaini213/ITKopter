# Security Policy

## Reporting a vulnerability

Please **do not** open a public GitHub issue for security vulnerabilities.

Instead, email **slafurrystudios@gmail.com** with:

- A description of the issue and its potential impact
- Steps to reproduce or a proof of concept, if available
- Affected version/commit, if known

We will acknowledge receipt within **72 hours** and keep you updated on our progress. Please give us a reasonable window to fix and disclose the issue before publishing details.

## Supported versions

This project is a starter template under active development. Security fixes are applied to the latest `main` branch. Older commits/tags are not actively patched.

## Scope

This repository primarily contains Unity game code, editor tooling, and GitHub Actions workflows. Of particular interest:

- **GitHub Actions workflows** (`.github/workflows/`) — especially anything handling secrets, Google Drive access, or deploy credentials
- **Python asset tooling** (`retrieve.py`, `track.py`) — Drive downloads and API usage
- **Scripting/code execution** in the Unity project

When setting up this repository as a template, rotate/remove any example credentials and configure your own [Actions secrets](https://docs.github.com/en/actions/security-guides/using-secrets-in-github-actions). Never commit API keys, service-account JSON, or webhook URLs to the repository.

## Preferred languages

Write reports in English or Indonesian.
