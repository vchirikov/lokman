import React, { Component } from 'react';
import NavBar from 'components/NavBar/NavBar';
import LocksPage from 'components/LocksPage/LocksPage';

export class App extends Component {
  public render(): JSX.Element {
    return (
      <div class="flex flex-col flex-1">
        <NavBar />
        <div class="container mx-auto flex flex-col flex-1">
          <LocksPage />
        </div>
      </div>
    );
  }
}

export default App;
