// Usage: node run-experiment.js instances-file params-file program-file

const fs = require('fs');
const path = require('path');
const dir = process.cwd();
const { exec } = require("child_process");

/**
 * @returns {{
    params: { [key: string]: { name: string, values: any[] } }
    conditions: string[]
  }}
 */
const paramsProcessor = () => {
  const params = {};
  const conditions = [];
  const rawParams = fs.readFileSync(paramsFile).toString().split("\n").filter(s => s);

  rawParams.forEach(line => {
    let parts = line.replace(/\#.+/, '').split('~').map(s => s.trim());
    const param = { name: parts[0], values: [] };

    const opts = parts[1].split(";").map(s => s.trim());
    opts.forEach(opt => {
      if (opt.startsWith("range:")) {
        const [min, max] = opt.replace("range:", "").split("-").map(s => parseFloat(s));
        const step = parseFloat(opts.find(s => s.startsWith("step:")).replace("step:", ""));
        for (let i = min; i <= max; i += step) {
          i = parseFloat(i.toFixed(2));
          param.values.push(i);
        }
      } else if (opt.startsWith("condition:")) {
        const condition = opt.replace("condition:", "").trim();
        if (!conditions.includes(condition))
          conditions.push(condition);
      } else if (opt.startsWith("domain:")) {
        param.values = opt.replace("domain:", "").replace(/\[|\]|\s/g, '').split(',');
      }
    });
    params[param.name] = param;
  });

  return { params, conditions };
}

/**
 * @param {{ [key: string]: { name: String, values: any[], conditions: any[] } }} params
 * @param {string[]} conditions
 * @returns {{ [key: string]: any}[]}
 */
const permGenerator = (params, conditions) => {
  const keys = Object.keys(params);
  let perms = params[keys[0]].values.map(p => [p]);

  for (let i = 1; i < keys.length; i++) {
    const key = keys[i];
    const param = params[key];
    const _perms = [];

    for (let j = 0; j < perms.length; j++) {
      const perm = perms[j];
      for (let k = 0; k < param.values.length; k++) {
        _perms.push([...perm, param.values[k]]);
      }
    }

    perms = _perms;
  }

  perms = perms.map(perm => {
    const _perm = {};
    for (let i = 0; i < perm.length; i++)
      _perm[keys[i]] = perm[i];
    return _perm;
  });

  perms = perms.filter(data => {
    for (let i = 0; i < conditions.length; i++) {
      const condition = conditions[i].replace(/([a-zA-Z0-9\-]+)/g, `data['$1']`);
      if (!eval(`(() => ${condition})()`))
        return false;
    }
    return true;
  })

  return perms;
}


// MAIN

const [_, paramsFile, programFile] = process.argv.slice(2).map(f => {
  if (f.startsWith('./'))
    return `${dir}/${f.replace('./', '')}`;
  if (!f.startsWith('/') && !f.startsWith('~') && !f.startsWith('ftp') && !f.startsWith('http'))
    return `${dir}/${f}`;
  return f;
});
const instanceFile = process.argv[2];

if (!instanceFile || !paramsFile) {
  console.log("Usage: node run-experiment.js instances-file params-file")
  process.exit(-1);
}
const logFile = `${__dirname}/output/${path.basename(instanceFile)}.log`;


const { params, conditions } = paramsProcessor();
const perms = permGenerator(params, conditions);

(async () => {
  let i = 0; // adjust starting point here
  const maxThreads = 20; // 200 paralel executions
  const start = new Date().valueOf();

  while (i < perms.length) {
    const end = (i + maxThreads >= perms.length) ? perms.length : (i + maxThreads);
    const promises = [];
    const execResults = {};

    for (let index = i; index < end; index++) {
      const perm = perms[index];
      let cmd = `${programFile} --file ${instanceFile} --skip-file-log`;
      let cmdOptions = "{";

      for (const key in perm) {
        cmd += ` --${key} ${perm[key]}`;
        cmdOptions += `"${key}":"${perm[key]}",`;
      }
      cmdOptions = cmdOptions.replace(/\,$/g, '') + "}";
      execResults[cmdOptions] = [];

      for (let trie = 0; trie < 10; trie++) {
        promises.push(new Promise((resolve, reject) => {
          exec(`${cmd}`, (err, stdout, stderr) => {
            if (err) {
              console.error("error", perm, err);
              resolve(true);
            } else {
              let res = stdout.split("\n").filter(s => s);
              res = res[res.length - 1];

              const [score, time, iters, sol] = res.split(";");
              execResults[cmdOptions].push({
                score: parseFloat(score),
                time: parseFloat(time),
                iters: parseInt(iters),
                sol
              });
              resolve(true);
            }
          });
        }));
      }
    }

    await Promise.all(promises);
    for (const key in execResults) {
      const elems = execResults[key];
      let avgScore = elems[0].score;
      let avgTime = elems[0].time;
      let best = elems[0];

      for (let index = 1; index < elems.length; index++) {
        const elem = elems[index];
        if (elem.score > best.score)
          best = elem;
        avgScore += (elem.score);
        avgTime += (elem.time);
      }

      avgScore = avgScore / elems.length;
      avgTime = avgTime / elems.length;

      const output = `${key};${avgScore};${avgTime};${best.score};${best.time};${best.iters};${best.sol}\n`;
      fs.writeFile(logFile, output, { flag: 'a' }, () => null);
    }

    i = end;
    fs.writeFile(logFile, `Permutation ${i} reached\n`, { flag: 'a' }, () => null);
    console.log(end);
  }

  console.log("exec time: ", (new Date().valueOf() - start) / 1000);
})();

// total: 79200