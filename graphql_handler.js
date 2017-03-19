/* eslint-disable */
'use strict';

module.exports.graphql = (event, context, callback) => {

  const jsonBody = {
    msg: "Hello World"
  }

   const response = {
    statusCode: 200,
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify(jsonBody),
  };

  // callback is sending HTML back
  callback(null, response);
};
