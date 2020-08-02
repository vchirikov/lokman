import React, { Component } from 'react';
import { NavBar } from 'components/NavBar/NavBar';

export class App extends Component {
  public render(): JSX.Element {
    return (
      <div>
        <NavBar />
        <div className="container mx-auto bg-blue-200">
          Eto vovan111!
        </div>
      </div>
    );
  }
}

export default App;
