// Usage: node run-brute-force.js instances-file params-file program-file
// Example: node ./run-brute-force.js ../instances/MCTOPP-Solomon/MCTOPMTWP-4-rc108-out.txt ../program

const dir = process.cwd();
const { exec } = require("child_process");

// MAIN

const [_, programFile] = process.argv.slice(2).map(f => {
  if (f.startsWith('./'))
    return `${dir}/${f.replace('./', '')}`;
  if (!f.startsWith('/') && !f.startsWith('~') && !f.startsWith('ftp') && !f.startsWith('http'))
    return `${dir}/${f}`;
  return f;
});
const instanceFile = process.argv[2];

if (!instanceFile || !programFile) {
  console.log("Usage: node run-experiment.js instances-file program-file")
  process.exit(-1);
}


const TIMEOUT = 48 * 60 * 60 * 1000; // adjust timeout here
const start = new Date().valueOf();
console.log('start time: ', start)

const cmd = `${programFile} --semi-force --file ${instanceFile}`;

let timeout;

const proc = exec(`${cmd}`, (err, stdout, stderr) => {
  console.log("exec time:  ", (new Date().valueOf() - start) / 1000);
  clearTimeout(timeout);
  if (err) console.error("error", stderr);
});

timeout = setTimeout(() => {
  console.log('child process killed after timeout')
  console.log("exec time:  ", (new Date().valueOf() - start) / 1000);
  proc.kill('SIGKILL');
}, TIMEOUT)

