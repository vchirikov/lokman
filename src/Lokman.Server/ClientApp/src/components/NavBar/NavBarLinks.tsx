import React from 'react';

export type NavBarLinksItem = {
  content: React.ReactNode;
  url: string;
  openNewTab: boolean;
};


interface Props {
  items: NavBarLinksItem[];
}

const NavBarLinks = ({ items }: Props) => {
  const borderColor: string = "blue";
  const linkCssClasses = `py-1 px-0 lg:px-2 block border-b-2 border-transparent hover:border-${borderColor}-400`;

  var links = items.map((item, idx) =>
    <li key={idx}>
      <a class={linkCssClasses} href={item.url.startsWith("http") ? item.url : `${process.env.PUBLIC_URL}${item.url}`} rel={item.openNewTab ? "nofollow noopener noreferrer" : ""} target={item.openNewTab ? "_blank" : "_self"}>
        {item.content}
      </a>
    </li>
  );
  return <nav><ul class="lg:flex items-center justify-between text-gray-700 pt-3 lg:pt-0 text-xl">{links}</ul></nav>
};

export default NavBarLinks;
