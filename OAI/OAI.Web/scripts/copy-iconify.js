const fs = require('fs');
const path = require('path');

const source = path.join(__dirname, '..', 'node_modules', 'iconify-icon', 'dist', 'iconify-icon.min.js');
const targetDirectory = path.join(__dirname, '..', 'wwwroot', 'vendor', 'iconify');
const target = path.join(targetDirectory, 'iconify-icon.min.js');

fs.mkdirSync(targetDirectory, { recursive: true });
fs.copyFileSync(source, target);

console.log(`Copied ${source} to ${target}`);
