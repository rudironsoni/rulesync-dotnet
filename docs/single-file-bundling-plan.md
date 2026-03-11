# Single-File Executable Bundling Analysis

## Overview

Analysis of bundling Node.js + rulesync into native executables to eliminate the Node.js installation requirement.

## Tool Comparison

| Tool | Status | Pros | Cons |
|------|--------|------|------|
| **pkg** | Deprecated | Mature | No longer maintained by Vercel |
| **nexe** | Stalled | Cross-platform | Limited maintenance |
| **Node.js SEA** | Official | Native feature | Requires Node 20+, complex code signing |
| **Bun** | Active | Fast, modern | Limited platform support |

## Size Comparison

| Approach | Size per Platform | Total (4 platforms) |
|----------|------------------|---------------------|
| Current (JS only) | ~2MB | ~2MB (shared) |
| Node.js SEA | ~45MB | ~180MB |
| Bun compile | ~35MB | ~140MB |

## Recommendation: Keep Current JS Bundling

### Current Approach ✅

**Pros:**
- Small package size (~2MB)
- Single package for all platforms
- Already implemented and working
- Simple build process

**Cons:**
- Requires Node.js runtime (but this is usually available)

### Native Executable Approach

**Pros:**
- Zero external dependencies
- Deterministic runtime

**Cons:**
- 80-100x larger package size
- Must build for each platform
- Code signing complexity on macOS
- Slower CI builds
- Antivirus false positives common

## Conclusion

The current JS bundling is the right choice. It solves the core problem (no npm install needed) while keeping the package small. Native executables are a valid future enhancement but add complexity for marginal benefit.
