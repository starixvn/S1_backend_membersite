"use strict";
function number_format(n) {
  n += "";
  var arr = n.split(".");
  var re = /(\d{1,3})(?=(\d{3})+$)/g;
  return arr[0].replace(re, "$1,") + (arr.length == 2 ? "." + arr[1] : "");
}
