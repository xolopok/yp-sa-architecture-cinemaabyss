const newman = require('newman');
const fs = require('fs');
const path = require('path');
const { parseArgs } = require('node:util');

// Parse command line arguments
const argv = parseArgs({
  options: {
    environment: { type: 'string', short: 'e', default: 'local' },
    collection: { type: 'string', short: 'c', default: 'CinemaAbyss' },
    folder: { type: 'string', short: 'f' },
    reporters: { type: 'string', short: 'r', default: 'cli,htmlextra,junit' },
    bail: { type: 'boolean', short: 'b', default: false },
    timeout: { type: 'string', short: 't', default: '10000' },
  },
  strict: false,
  allowPositionals: true,
}).values;

// Create reports directory if it doesn't exist
const reportsDir = path.join(__dirname, 'reports');
if (!fs.existsSync(reportsDir)) {
  fs.mkdirSync(reportsDir, { recursive: true });
}

// Configure Newman run
const collectionPath = path.join(__dirname, `${argv.collection}.postman_collection.json`);
const environmentPath = path.join(__dirname, `${argv.environment}.environment.json`);

// Validate files exist
if (!fs.existsSync(collectionPath)) {
  console.error(`Collection file not found: ${collectionPath}`);
  process.exit(1);
}

if (!fs.existsSync(environmentPath)) {
  console.error(`Environment file not found: ${environmentPath}`);
  process.exit(1);
}

// Resolve reporters.
const builtinReporters = new Set(['cli', 'json', 'junit', 'progress', 'silent']);
const requestedReporters = argv.reporters.split(',').map(r => r.trim()).filter(Boolean);
const reporters = requestedReporters.filter(name => {
  if (builtinReporters.has(name)) return true;
  try {
    require.resolve(`newman-reporter-${name}`);
    return true;
  } catch {
    console.warn(`Reporter '${name}' is not installed; skipping. Install with: npm i newman-reporter-${name}`);
    return false;
  }
});
if (reporters.length === 0) {
  reporters.push('cli');
}

const timestamp = new Date().toISOString().replace(/:/g, '-');

// Configure Newman options
const newmanOptions = {
  collection: require(collectionPath),
  environment: require(environmentPath),
  reporters: reporters,
  reporter: {
    htmlextra: {
      export: path.join(reportsDir, `report-${argv.environment}-${timestamp}.html`),
      showOnlyFails: false,
      noSyntaxHighlighting: false,
      testPaging: true,
      browserTitle: "CinemaAbyss API Test Report",
      title: "CinemaAbyss API Test Report",
      titleSize: 1,
      omitHeaders: false
    },
    junit: {
      export: path.join(reportsDir, `junit-report-${argv.environment}-${timestamp}.xml`)
    }
  },
  bail: argv.bail,
  timeoutRequest: Number(argv.timeout) || 10000,
  delayRequest: 100 // Small delay between requests
};

// Add folder option if specified
if (argv.folder) {
  newmanOptions.folder = argv.folder;
}

// Run Newman
console.log(`Running tests against ${argv.environment} environment...`);
newman.run(newmanOptions, function (err, summary) {
  if (err) {
    console.error('Error running Newman:', err);
    process.exit(1);
  }

  // Log results
  console.log('Newman run completed!');

  const failureCount = summary.run.failures.length;
  console.log(`Total requests: ${summary.run.stats.requests.total}`);
  console.log(`Failed requests: ${summary.run.stats.requests.failed}`);
  console.log(`Total assertions: ${summary.run.stats.assertions.total}`);
  console.log(`Failed assertions: ${summary.run.stats.assertions.failed}`);

  // Exit with appropriate code
  process.exit(failureCount > 0 ? 1 : 0);
});
