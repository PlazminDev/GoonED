const Keys =
{
  Space:32,
  Q:81,
  W:87,
  E:69,
  R:82,
  T:84,
  Y:89,
  U:85,
  I:73,
  O:79,
  P:80,
  A:65,
  S:83,
  D:68,
  F:70,
  G:71,
  H:72,
  J:74,
  K:75,
  L:76,
  Z:90,
  X:88,
  C:67,
  V:86,
  B:66,
  N:78,
  M:77,
  Shift:16,
  Control:17,
  Left:37,
  Right:39,
  Up:38,
  Down:40,
  Escape:27,
  Plus:61,
  Minus:173,
};

var keys = [];

document.addEventListener('keydown', handleKeyAction);
document.addEventListener('keyup', handleKeyAction);

function Input() { }

const GetKey = function(keyCode) {
  return keys.includes(keyCode);
};

function handleKeyAction(event) {
  if(event.type == "keydown" && !keys.includes(event.keyCode)) {
    keys.push(event.keyCode);
  }
  if(event.type == "keyup") {
    for(var index = 0; index < keys.length; index++) {
      if(keys[index] === event.keyCode) {
        keys.splice(index, 1);
        break;
      }
    }
  }
}