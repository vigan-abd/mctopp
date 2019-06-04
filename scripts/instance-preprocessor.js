// Usage: node instance-preprocessor.js file1 file2...

const fs = require('fs');
const dir = process.cwd();

/**
 * @param {String} line
 * @returns {String} 
 */
const parsePoi = (line) => {
  line = line.split(' ');

  if (line.length != 22) throw new Error(`Item with id ${line[0]} has incorrect length: ${line.length}`);

  line = [...line.slice(0, 6), line[9], ...line.slice(11)];
  
  let typeValid = false;
  for (let i = line.length - 1; i >= line.length - 10; i--) {
    if(line[i] == '1') {
      typeValid = true;
      break;  
    }
  }

  if (!typeValid) throw new Error(`Item with id ${line[0]} has incorrect type`);

  line = line.join(' ');
  return line;
}

const files = process.argv.slice(2).map(f => {
  if (f.startsWith('./'))
    return `${dir}/${f.replace('./', '')}`;
  if (!f.startsWith('/') && !f.startsWith('~') && !f.startsWith('ftp') && !f.startsWith('http'))
    return `${dir}/${f}`;
  return f;
});


try {
  files.forEach(f => {
    let content = fs.readFileSync(f).toString().split("\n").filter(line => line);
    let tourCount = 0;
    for (let i = 0; i < content.length; i++) {
      const line = content[i];
      if (i == 0) {
        tourCount = parseInt(line.split(' ')[0]);
      } else if (i > 3 + tourCount) {
        content[i] = parsePoi(line);
      }
    }

    content = content.join("\n");

    const output = f.split('.');
    const ext = output.pop();
    output[output.length - 1] = `${output[output.length - 1]}-out`;
    output.push(ext);

    fs.writeFileSync(`${output.join('.')}`, content);
  });
} catch (err) {
  console.error(err);
}