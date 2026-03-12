#!/usr/bin/env node
/**
 * Build native executables from bundled rulesync using Bun
 * This script runs during SDK build to create platform-specific binaries.
 *
 * Requirements: Bun must be installed (https://bun.sh/)
 */

const { execFileSync } = require('child_process');
const fs = require('fs');
const path = require('path');

const RULESYNC_SUBMODULE = path.join(__dirname, '..', '..', 'rulesync');
const OUTPUT_DIR = path.join(__dirname, '..', '..', 'native-binaries');

// Timeout for executable verification (milliseconds)
const VERIFICATION_TIMEOUT_MS = 10000;

// Platform mappings shared across all functions
const PLATFORM_MAPPINGS = {
    // Node.js platform -> internal platform name
    nodeToInternal: {
        'win32': 'windows',
        'darwin': 'darwin',
        'linux': 'linux'
    },
    // Internal platform -> runtime ID prefix
    toRuntimeId: {
        'windows': 'win',
        'darwin': 'osx',
        'linux': 'linux'
    }
};

// All supported platforms for cross-compilation
const ALL_PLATFORMS = [
    { platform: 'linux', arch: 'x64', target: 'bun-linux-x64', exeName: 'rulesync' },
    { platform: 'darwin', arch: 'x64', target: 'bun-darwin-x64', exeName: 'rulesync' },
    { platform: 'darwin', arch: 'arm64', target: 'bun-darwin-arm64', exeName: 'rulesync' },
    { platform: 'windows', arch: 'x64', target: 'bun-windows-x64', exeName: 'rulesync.exe' }
];

function main() {
    const args = process.argv.slice(2);
    const buildAll = args.includes('--all') || args.includes('-a');

    console.log('Building native executables for rulesync using Bun...');

    // Check for Bun
    const bunPath = findBun();
    if (!bunPath) {
        console.error('Error: Bun is not installed.');
        console.error('  Install from: https://bun.sh/');
        console.error('  Or run: curl -fsSL https://bun.sh/install | bash');
        process.exit(1);
    }
    console.log('Using Bun:', bunPath);

    // Ensure rulesync is built
    const cliJsPath = path.join(RULESYNC_SUBMODULE, 'dist', 'cli', 'index.js');
    if (!fs.existsSync(cliJsPath)) {
        console.error('Error: rulesync dist not found. Build the submodule first:');
        console.error('  cd rulesync && pnpm install && pnpm run build');
        process.exit(1);
    }

    // Create output directory
    if (!fs.existsSync(OUTPUT_DIR)) {
        fs.mkdirSync(OUTPUT_DIR, { recursive: true });
    }

    if (buildAll) {
        console.log('\nBuilding native executables for ALL platforms...\n');
        let successCount = 0;
        let failCount = 0;

        for (const config of ALL_PLATFORMS) {
            try {
                buildPlatform(bunPath, cliJsPath, config);
                successCount++;
            } catch (error) {
                console.error(`Failed to build for ${config.platform}-${config.arch}:`, error.message);
                failCount++;
            }
        }

        console.log('\n' + '='.repeat(60));
        console.log('Build Summary:');
        console.log('  Successful:', successCount);
        console.log('  Failed:', failCount);
        console.log('  Total:', ALL_PLATFORMS.length);

        if (failCount > 0) {
            process.exit(1);
        }
    } else {
        // Build only current platform
        const platform = process.platform;
        const arch = process.arch;
        const normalizedPlatform = normalizePlatform(platform);
        const runtimeId = getRuntimeId(platform, arch);

        const config = ALL_PLATFORMS.find(p =>
            p.platform === normalizedPlatform && p.arch === arch
        );

        if (!config) {
            console.error('Unsupported platform/architecture:', platform, arch);
            process.exit(1);
        }

        buildPlatform(bunPath, cliJsPath, config);
    }
}

/**
 * Normalizes Node.js platform strings to our internal platform names.
 * @param {string} platform - Node.js process.platform value
 * @returns {string} Normalized platform name ('darwin', 'linux', 'windows')
 */
function normalizePlatform(platform) {
    return PLATFORM_MAPPINGS.nodeToInternal[platform] || platform;
}

function buildPlatform(bunPath, cliJsPath, config) {
    const runtimeId = getRuntimeIdFromConfig(config);
    const platformDir = path.join(OUTPUT_DIR, runtimeId);

    console.log(`Building for ${runtimeId}...`);
    console.log('  Target:', config.target);

    if (!fs.existsSync(platformDir)) {
        fs.mkdirSync(platformDir, { recursive: true });
    }

    const outputExe = path.join(platformDir, config.exeName);
    console.log('  Output:', outputExe);

    try {
        execFileSync(bunPath, [
            'build',
            '--compile',
            '--target=' + config.target,
            cliJsPath,
            '--outfile',
            outputExe
        ], {
            stdio: 'inherit',
            cwd: process.cwd()
        });
    } catch (error) {
        throw new Error('Compilation failed: ' + error.message);
    }

    // Make executable (skip for Windows .exe on Unix)
    if (!config.exeName.endsWith('.exe') || process.platform !== 'win32') {
        fs.chmodSync(outputExe, 0o755);
    }

    // Verify the executable (only for current platform)
    const currentPlatform = process.platform === 'darwin' ? 'darwin' : process.platform === 'win32' ? 'windows' : 'linux';
    if (config.platform === currentPlatform && config.arch === process.arch) {
        console.log('  Verifying executable...');
        try {
            const result = execFileSync(outputExe, ['--version'], {
                encoding: 'utf8',
                timeout: VERIFICATION_TIMEOUT_MS
            });
            console.log('    Version:', result.trim());
        } catch (error) {
            console.error('    Warning: Verification failed:', error.message);
        }
    }

    const stats = fs.statSync(outputExe);
    console.log('  Size:', (stats.size / 1024 / 1024).toFixed(2), 'MB');
    console.log('');
}

function findBun() {
    // Check PATH for bun using execFileSync
    try {
        const result = execFileSync('which', ['bun'], { encoding: 'utf8' }).trim();
        if (result && fs.existsSync(result)) {
            return result;
        }
    } catch {
        // not found in PATH
    }

    // Check common locations
    const commonPaths = [
        path.join(process.env.HOME || '', '.bun', 'bin', 'bun'),
        '/usr/local/bin/bun',
        '/usr/bin/bun'
    ];

    for (const p of commonPaths) {
        if (fs.existsSync(p)) {
            return p;
        }
    }

    return null;
}

/**
 * Gets the runtime identifier for a Node.js platform/arch combination.
 * @param {string} platform - Node.js process.platform value
 * @param {string} arch - Node.js process.arch value
 * @returns {string} Runtime ID (e.g., 'win-x64', 'linux-arm64')
 */
function getRuntimeId(platform, arch) {
    const normalized = normalizePlatform(platform);
    const prefix = PLATFORM_MAPPINGS.toRuntimeId[normalized] || normalized;
    return prefix + '-' + arch;
}

/**
 * Gets the runtime identifier from a platform config object.
 * @param {Object} config - Platform configuration object
 * @param {string} config.platform - Internal platform name
 * @param {string} config.arch - Architecture
 * @returns {string} Runtime ID (e.g., 'osx-arm64', 'win-x64')
 */
function getRuntimeIdFromConfig(config) {
    const prefix = PLATFORM_MAPPINGS.toRuntimeId[config.platform] || config.platform;
    return prefix + '-' + config.arch;
}

main();
