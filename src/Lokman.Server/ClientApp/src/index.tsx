import React from 'react';
import ReactDOM from 'react-dom';
import { Provider } from 'react-redux';
import 'style/spinkit.css';
import 'style/tailwind.css';
import '../node_modules/fork-awesome/css/fork-awesome.min.css';
import * as serviceWorker from './serviceWorker';
import App from 'app/App';
import store from 'app/store';

const rootElement = document.getElementById('root');
ReactDOM.render(
  <React.StrictMode>
    <Provider store={store}>
      <App />
    </Provider>
  </React.StrictMode>,
  rootElement
);

// If you want your app to work offline and load faster, you can change
// unregister() to register() below. Note this comes with some pitfalls.
// Learn more about service workers: https://bit.ly/CRA-PWA
serviceWorker.unregister();
